using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using Newtonsoft.Json;
using Npgsql;
using Serilog;

namespace MagnumBI.Dispatch.Engine.Datastore {
    public class PostgreSql : BaseDatastore {
        private const int AlreadyExistsErrorCode = -2147467259;
        private const string TableName = "Jobs";
        private const int MaxAttempts = 3;
        private const int RetryDelayMilliSeconds = 2000;
        private readonly NpgsqlConnection connection;
        private readonly string dbName = "magnumbidispatch";

        public PostgreSql(PostgreSqlConfig config) {
            string adminConnString = this.ConstructConnectionString(config, true);
            // Make admin connection to ensure database exists
            NpgsqlConnection adminConnection = new NpgsqlConnection(adminConnString);
            adminConnection.Open();
            // Check valid database name
            if (!string.IsNullOrWhiteSpace(config.PostgreSqlDb)) {
                this.dbName = config.PostgreSqlDb;
            }
            this.EnsureDbExists(adminConnection);
            adminConnection.Close();

            string connString = this.ConstructConnectionString(config);
            this.connection = new NpgsqlConnection(connString);
            this.SetupSsl(config);
            this.connection.Open();

            if (this.connection.State == ConnectionState.Broken || this.connection.State == ConnectionState.Closed) {
                Log.Error("Failed to connect to PostgreSQL, is config correct?");
                throw new Exception("Failed to connect to PostgreSQL, is config correct?");
            }
            this.EnsureTableExists();
        }

        /// <summary>
        ///     Compose the connection string to either the admin or MagnumBI Dispatch database.
        /// </summary>
        /// <param name="config">Config for connection</param>
        /// <param name="admin">True if we want to connect to the admin database</param>
        /// <returns>Connection string for PostgreSQL.</returns>
        private string ConstructConnectionString(PostgreSqlConfig config, bool admin = false) {
            string[] split = config.PostgreSqlHostnames[0].Split(':');
            string ip = split[0];
            int port = int.Parse(split[1]);

            string connString = $"Server={ip};Port={port};" +
                                $"User Id={config.PostgreSqlUser};Password={config.PostgreSqlPassword};" +
                                $"Database={(admin ? config.PostgreSqlAdminDb : this.dbName)};";

            // Set up SSL if applicable
            if (config.SslConfig.UseSsl) {
                if (config.SslConfig != null) {
                    connString += $"SSL Mode=Required;Trust Server Certificate={!config.SslConfig.VerifySsl};";
                } else {
                    throw new Exception("SSL use was requested in config but no SslConfig was defined.");
                }
            }

            // Timeout settings
            connString += "Timeout=8;";

            return connString;
        }

        private void SetupSsl(PostgreSqlConfig config) {
            this.connection.ProvideClientCertificatesCallback = clientCerts => {
                this.ProvideClientCertificates(clientCerts, config);
            };
//            this.connection.UserCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        private void ProvideClientCertificates(X509CertificateCollection clientCerts, PostgreSqlConfig config) {
            foreach (KeyValuePair<string, string> sslConfigClientCertificate in
                config.SslConfig.ClientCertificates) {
                if (!File.Exists(sslConfigClientCertificate.Key)) {
                    Log.Error("Could not find cert file, skipping", sslConfigClientCertificate);
                    continue;
                }
                clientCerts.Add(new X509Certificate2(sslConfigClientCertificate.Key,
                    sslConfigClientCertificate.Value));
            }
        }

        private void EnsureDbExists(NpgsqlConnection adminConnection) {
            string commandText = $"CREATE DATABASE {this.dbName}";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, adminConnection);
            try {
                cmd.ExecuteNonQuery();
            } catch (PostgresException e) {
                if (e.ErrorCode == AlreadyExistsErrorCode) {
                    // Database already exists, carry on
                    return;
                }
                e.MessageText += ". Occurred ensuring db exists.";
                throw e;
            }
            Log.Debug($"PostgreSQL database {this.dbName} did not exist. New database created.");
        }

        /// <summary>
        ///     Checks if the default table exists. If it doesn't, create it.
        /// </summary>
        private void EnsureTableExists() {
            NpgsqlCommand cmd = new NpgsqlCommand();
            cmd.Connection = this.connection;
            cmd.CommandText =
                $"CREATE TABLE {TableName} (\n" +
                "    job_id text UNIQUE NOT NULL,\n" +
                "    qid text NOT NULL,\n" +
                "    data text NOT NULL,\n" +
                "    prev_job_ids text NOT NULL,\n" +
                "    PRIMARY KEY(qid, job_id)\n" +
                ")";
            try {
                cmd.ExecuteNonQuery();
            } catch (PostgresException e) {
                if (e.ErrorCode == AlreadyExistsErrorCode) {
                    // Table already exists, carry on
                    return;
                }
                e.MessageText += ". Occurred ensuring table exists.";
                throw e;
            }
            Log.Debug($"PostgreSQL table {TableName} did not exist. New table created.");
        }

        /// <inheritdoc />
        public override void Add(string qid, Job job) {
            PostgresJob pgJob = PostgresJob.ConvertJob(job);
            dynamic data = JsonConvert.SerializeObject(pgJob.Data);
            string prevJobIds = JsonConvert.SerializeObject(pgJob.PreviousJobIds);
            string commandText =
                $"INSERT INTO {TableName}\n" +
                $"  VALUES ('{pgJob.JobId}', '{qid}', '{data}', '{prevJobIds}')\n" +
                $"ON CONFLICT (job_id)\n" +
                $"DO\n" +
                $"  UPDATE\n" +
                $"      SET data = '{data}', prev_job_ids = '{prevJobIds}'";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
            cmd.ExecuteNonQuery();
        }

        /// <inheritdoc />
        public override Job Get(string qid, string jobId) {
            NpgsqlDataReader reader = null;
            try {
                // Attempt to get job
                Log.Debug("Retrieving job", qid, jobId);
                string commandText =
                    $"BEGIN TRANSACTION;" +
                    $"  SELECT job_id, qid, data, prev_job_ids FROM {TableName}\n" +
                    $"  WHERE job_id = '{jobId}' AND qid = '{qid}';" +
                    $"END TRANSACTION;";
                NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
                reader = cmd.ExecuteReader();

                // Access results
                if (!reader.HasRows) {
                    throw new Exception($"Did not find any results for {jobId} on {qid}");
                }
                reader.Read();
                return PostgresJob.JobFromReader(reader);
            } catch (Exception e) {
                throw new Exception($"PostgreSQL: Failed to get Job: {qid},{jobId}", e);
            } finally {
                reader?.Close();
            }
        }

        /// <inheritdoc />
        public override void Remove(string qid, string jobId) {
            string commandText =
                $"DELETE FROM {TableName}\n" +
                $"  WHERE job_id = '{jobId}' AND qid = '{qid}'";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
            cmd.ExecuteNonQuery();
        }

        /// <inheritdoc />
        public override void ClearData() {
            string commandText = $"DELETE FROM {TableName}";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
            cmd.ExecuteNonQuery();
        }

        /// <inheritdoc />
        public override void Close() {
            this.connection.Close();
        }

        /// <inheritdoc />
        public override void ClearQueue(string qid) {
            string commandText = $"DELETE FROM {TableName} WHERE qid = '{qid}'";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
            cmd.ExecuteNonQuery();
        }

        /// <inheritdoc />
        public override bool IsEmpty(string qid) {
            string commandText = $"SELECT Count(*) FROM {TableName}";
            NpgsqlCommand cmd = new NpgsqlCommand(commandText, this.connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            int result = reader.GetInt32(0);
            reader.Close();
            return result == 0;
        }

        /// <summary>
        ///     A Job which can be stored in a PostgreSQL database.
        ///     TODO probably remove and just use Job.
        /// </summary>
        [JsonObject(MemberSerialization.OptOut,
            ItemTypeNameHandling =
                TypeNameHandling.None)]
        public class PostgresJob : Job {
            /// <summary>
            ///     The private part of the job id.
            /// </summary>
            private string jobId;

            /// <summary>
            ///     ID for this job.
            /// </summary>
            public new string JobId {
                get {
                    if (this.jobId != null) {
                        return this.jobId;
                    }
                    this.jobId = MagnumBiDispatchController.NewJobId();
                    return this.jobId ?? "";
                }
                private set => this.jobId = value;
            }

            /// <summary>
            ///     Default constructor for MongoJob.
            /// </summary>
            /// <param name="data">Job data</param>
            /// <param name="jobId">Job id</param>
            /// <param name="startDateTime">Time job is due to start</param>
            /// <param name="previousJobIds">Any previous IDs for this job</param>
            public PostgresJob(dynamic data,
                string jobId = null,
                DateTime? startDateTime = null,
                params string[] previousJobIds) : base(null, jobId, startDateTime, previousJobIds) {
                this.Data = data;
                this.jobId = jobId;
                if (this.PreviousJobIds == null) {
                    this.PreviousJobIds = new string[] {
                    };
                }
            }

            /// <summary>
            ///     Converts a Job to a PostgresJob.
            /// </summary>
            /// <param name="job">Job to convert</param>
            /// <returns>The PostgresJob equivalent of the given job.</returns>
            public static PostgresJob ConvertJob(Job job) {
                PostgresJob postgresJob = new PostgresJob(job.Data, job.JobId, null, job.PreviousJobIds);
                postgresJob.StartDateTime = job.StartDateTime;
                return postgresJob;
            }

            /// <summary>
            ///     Converts this PostgresJob to a Job.
            /// </summary>
            /// <returns>The Job equivalent of this PostgresJob.</returns>
            public Job ToJob() {
                return new Job(this.Data, this.jobId, null, this.PreviousJobIds) {
                    StartDateTime = this.StartDateTime
                };
            }

            public static Job JobFromReader(NpgsqlDataReader reader) {
                string jobId = reader.GetString(0);
                dynamic data = JsonConvert.DeserializeObject(reader.GetString(2));
                string[] prevJobIds = JsonConvert.DeserializeObject<string[]>(reader.GetString(3));
                return new Job(data, jobId, null, prevJobIds);
            }
        }
    }
}