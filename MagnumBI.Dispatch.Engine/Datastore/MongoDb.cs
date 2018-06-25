// 
// 0918
// 201709024:23 PM

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;

namespace MagnumBI.Dispatch.Engine.Datastore {
    /// <summary>
    ///     Database implementation for MongoDB.
    /// </summary>
    public class MongoDb : BaseDatastore {
        private const string TablePrefix = "Dispatch_";
        private const int MaxAttempts = 3;
        private const int RetryDelayMilliSeconds = 2000;
        private readonly IMongoDatabase database;

        /// <summary>
        ///     Default constructor for MongoDb.
        /// </summary>
        /// <param name="config">Configurations for database</param>
        public MongoDb(MongoDbConfig config) {
            MongoClientSettings mongoClientSettings = new MongoClientSettings {
                Credential =
                    MongoCredential.CreateCredential(config.MongoAuthDb, config.MongoUser, config.MongoPassword),
                SocketTimeout = TimeSpan.FromSeconds(30),
                ConnectTimeout = TimeSpan.FromSeconds(8),
                ServerSelectionTimeout = TimeSpan.FromSeconds(30)
            };

            // Set up replica set if applicable
            if (config.UseReplicaSet) {
                List<MongoServerAddress> addresses = new List<MongoServerAddress>();
                foreach (string hostname in config.MongoHostnames) {
                    string[] split = hostname.Split(':');
                    addresses.Add(new MongoServerAddress(split[0], int.Parse(split[1])));
                }

                mongoClientSettings.Servers = addresses;
                mongoClientSettings.ConnectionMode = ConnectionMode.Automatic;
                mongoClientSettings.ReplicaSetName = config.ReplicaSetName;
            } else {
                string[] split = config.MongoHostnames[0].Split(':');
                mongoClientSettings.Server = new MongoServerAddress(split[0], int.Parse(split[1]));
            }

            // Set up SSL if applicable
            if (config.SslConfig != null && config.SslConfig.UseSsl) {
                SetupSsl(config, mongoClientSettings);
            }

            // Access client and database
            MongoClient client = new MongoClient(mongoClientSettings);
            string dbName = !string.IsNullOrWhiteSpace(config.MongoCollection)
                ? config.MongoCollection
                : "MagnumBIDispatch";
            this.database = client.GetDatabase(dbName);
            bool isMongoLive = this.database.RunCommandAsync((Command<BsonDocument>) "{ping:1}").Wait(5000);
            if (!isMongoLive) {
                Log.Error("Failed to connect to MongoDB, is config correct?");
                throw new Exception("Failed to connect to MongoDB, is config correct?");
            }
        }

        /// <summary>
        ///     Set's up the ssl component of the configuration.
        /// </summary>
        /// <param name="config">MongoDB configurations</param>
        /// <param name="mongoClientSettings">Settings for the MongoDB client</param>
        private static void SetupSsl(MongoDbConfig config, MongoClientSettings mongoClientSettings) {
            mongoClientSettings.SslSettings = new SslSettings();
            mongoClientSettings.UseSsl = true;
            if (config.SslConfig.ClientCertificates != null && config.SslConfig.ClientCertificates.Count > 0) {
                // If there are client certificates then add them to the cert chain
                // TODO Support certificate authentication
                List<X509Certificate> certList = new List<X509Certificate>();
                foreach (KeyValuePair<string, string> sslConfigClientCertificate in
                    config.SslConfig.ClientCertificates) {
                    if (!File.Exists(sslConfigClientCertificate.Key)) {
                        Log.Error("Could not find cert file, skipping",
                            sslConfigClientCertificate);
                        continue;
                    }

                    certList.Add(new X509Certificate2(sslConfigClientCertificate.Key,
                        sslConfigClientCertificate.Value));
                }
            }

            if (!config.SslConfig.VerifySsl) {
                // Skip all certificate verification
                Log.Warning("Skipping mongodb certificate verification.");
                mongoClientSettings.SslSettings.CheckCertificateRevocation = false;
                mongoClientSettings.SslSettings.ServerCertificateValidationCallback =
                    (sender, certificate, chain, errors) => true;
            }
        }

        /// <inheritdoc />
        public override void Add(string qid, Job job) {
            this.Add(qid, job, false);
        }

        /// <summary>Overloaded method to allow second attempt if job contains invalid names.</summary>
        /// <inheritdoc cref="BaseDatastore.Add" />
        /// <param name="secondAttempt">Is this the second attempt to queue job?</param>
#pragma warning disable 1573
        public void Add(string qid, Job job, bool secondAttempt) {
#pragma warning restore 1573
            MongoJob mongoJob = null;
            try {
                mongoJob = MongoJob.ConvertJob(job);
                IMongoCollection<MongoJob> collection = this.GetCollection(qid);
                // If mongoJob already exists we need to replace it
                FilterDefinition<MongoJob> filterDefinition =
                    Builders<MongoJob>.Filter.Eq("_id", mongoJob.JobId);
                collection.ReplaceOne(filterDefinition,
                    mongoJob,
                    new UpdateOptions {
                        IsUpsert = true
                    });
            } catch (BsonSerializationException e) {
                if (secondAttempt) {
                    throw;
                }

                try {
                    if (e.Message.Contains("Element name")) {
                        // Name probs contains '.'
                        if (mongoJob != null) {
                            CheckForInvalidCharacters(ref mongoJob);
                            this.Add(qid, mongoJob, true);
                        }
                    }
                } catch (Exception) {
                    throw e;
                }
            } catch (Exception e) {
                Log.Error("Failed to add job data.", e);
                throw;
            }
        }

        /// <summary>
        ///     Attempts
        /// This method is slow so lets not use it too much.
        /// </summary>
        /// <param name="job"></param>
        private static void CheckForInvalidCharacters(ref MongoJob job) {
            Dictionary<string, object> changedData =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(job.Data));
            Dictionary<string, object> returnedData = CheckSectionForInvalidNames(changedData);


            dynamic thirdTime = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(returnedData));
            if (thirdTime != null) {
                job.Data = thirdTime;
            }
        }

        private static Dictionary<string, object> CheckSectionForInvalidNames(Dictionary<string, object> checker) {
            List<string> keyList = checker.Keys.ToList();
            foreach (string key in keyList) {
                Dictionary<string, object> changedData = null;
                try {
                    changedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(checker[key]));
                } catch (JsonSerializationException) {
                }
                if (changedData != null) {
                    checker[key] = CheckSectionForInvalidNames(changedData);
                }

                if (key.Contains(".")) {
                    string newKey = key.Replace(".", "_");
                    checker[newKey] = checker[key];
                    checker.Remove(key);
                }
            }

            return checker;
        }


        private IMongoCollection<MongoJob> GetCollection(string tableNamePostfix) {
            string tableName = TablePrefix + tableNamePostfix;
            return this.CreateTableIfNotExist(tableName);
        }

        /// <summary>
        ///     If a table doesn't already exist, create it.
        /// </summary>
        /// <param name="tableName">Name of table</param>
        /// <returns>Table with name tableName.</returns>
        private IMongoCollection<MongoJob> CreateTableIfNotExist(string tableName) {
            IMongoCollection<MongoJob> collection = this.database.GetCollection<MongoJob>(tableName);
            if (collection == null) {
                this.database.CreateCollection(tableName);
                collection = this.database.GetCollection<MongoJob>(tableName);
            }

            return collection;
        }

        /// <inheritdoc />
        public override void ClearData() {
            Log.Debug("Clearing Database.");
            List<BsonDocument> collections = this.database.ListCollections().ToList();
            foreach (BsonDocument collection in collections) {
                this.DeleteCollection(collection["name"].AsString);
            }
        }

        private void DeleteCollection(string collectionName) {
            this.database.DropCollection(collectionName);
        }

        /// <inheritdoc />
        public override Job Get(string qid, string jobId) {
            return this.AttemptGet(qid, jobId);
        }

        /// <summary>
        ///     Attempt to get a row from the datastore with the given job and queue ids.
        ///     Continues attempt until max number of attempts has been reached.
        /// </summary>
        /// <param name="qid">ID of queue job is on.</param>
        /// <param name="jobId">The job ID of the data you want.</param>
        /// <param name="attemptNum">Number of attempts made so far to get row. Should be 0 at first call.</param>
        /// <returns></returns>
        private Job AttemptGet(string qid, string jobId, int attemptNum = 0) {
            try {
                Log.Debug("Retrieving job", qid, jobId);
                IMongoCollection<MongoJob> collection = this.database.GetCollection<MongoJob>(TablePrefix + qid);
                FilterDefinition<MongoJob> filterDefinition =
                    Builders<MongoJob>.Filter.Eq("_id", jobId);
                List<MongoJob> queryResult = collection.Find(filterDefinition).ToList();
                if (queryResult.Count < 1) {
                    throw new Exception($"Did not find any results for {jobId} on {qid}");
                }

                return queryResult[0].ToJob();
            } catch (Exception e) {
                if (++attemptNum > MaxAttempts) {
                    throw new Exception($"MongoDB: Failed to get Job: {qid},{jobId}", e);
                }

                Log.Warning($"MongoDB: Failed to find job on attempt {attemptNum}, retrying after short delay.");
                Thread.Sleep(RetryDelayMilliSeconds);
                return this.AttemptGet(qid, jobId, attemptNum);
            }
        }

        /// <inheritdoc />
        public override bool IsEmpty(string qid) {
            IMongoCollection<Job> collection = this.database.GetCollection<Job>(TablePrefix + qid);
            return collection.Count(FilterDefinition<Job>.Empty) == 0;
        }

        /// <inheritdoc />
        public override void Close() {
        }

        /// <inheritdoc />
        public override void ClearQueue(string qid) {
            IMongoCollection<MongoJob> collection = this.database.GetCollection<MongoJob>(TablePrefix + qid);
            collection.DeleteMany(FilterDefinition<MongoJob>.Empty);
        }

        /// <inheritdoc />
        public override void Remove(string qid, string jobId) {
            Log.Debug("Removing job", qid, jobId);
            IMongoCollection<MongoJob> collection = this.database.GetCollection<MongoJob>(TablePrefix + qid);
            FilterDefinition<MongoJob> filterDefinition = Builders<MongoJob>.Filter.Eq("_id", jobId);
            DeleteResult deleteResult = collection.DeleteOne(filterDefinition);
            Log.Debug($"Deleted {deleteResult.DeletedCount} objects for {jobId} on {qid}");
        }
    }

    /// <summary>
    ///     A Job which can be stored in MongoDB.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut,
        ItemTypeNameHandling =
            TypeNameHandling.None)]
    public class MongoJob : Job {
        /// <summary>
        ///     The private part of the job id.
        /// </summary>
        private string jobId;

        /// <summary>
        ///     Default constructor for MongoJob.
        /// </summary>
        /// <param name="data">Job data</param>
        /// <param name="jobId">Job id</param>
        /// <param name="startDateTime">Time job is due to start</param>
        /// <param name="previousJobIds">Any previous IDs for this job</param>
        public MongoJob(dynamic data,
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
        ///     ID for this job.
        /// </summary>
        [BsonId]
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
        ///     Converts a Job to a MongoJob.
        /// </summary>
        /// <param name="job">Job to convert</param>
        /// <returns>The MongoJob equivalent of the given job.</returns>
        public static MongoJob ConvertJob(Job job) {
            MongoJob mongoJob = new MongoJob(job.Data, job.JobId, null, job.PreviousJobIds);
            mongoJob.StartDateTime = job.StartDateTime;
            return mongoJob;
        }

        /// <summary>
        ///     Converts this MongoJob to a Job.
        /// </summary>
        /// <returns>The Job equivalent of this MongoJob.</returns>
        public Job ToJob() {
            return new Job(this.Data, this.jobId, null, this.PreviousJobIds) {
                StartDateTime = this.StartDateTime
            };
        }
    }
}