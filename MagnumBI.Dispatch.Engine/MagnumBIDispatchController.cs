#region FileInfo

// MagnumBI.Dispatch MagnumBI.Dispatch.Engine MagnumBiDispatchController.cs
// Created: 20171016
// Edited: 20171016
// By: Timothy Gray (timgray)

#endregion

using System;
using JetBrains.Annotations;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using MagnumBI.Dispatch.Engine.Datastore;
using MagnumBI.Dispatch.Engine.Queue;
using Serilog;

namespace MagnumBI.Dispatch.Engine {
    /// <summary>
    ///     The main point of operation for the Mangum Microservices System.
    /// </summary>
    public sealed class MagnumBiDispatchController {
        private const int IdLength = 64;
        private const string ValidIdChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static MagnumBiDispatchController Instance;
        private static readonly Random RandomGen = new Random();

        /// <summary>
        ///     The datastore used by this controller.
        /// </summary>
        public IDatastore Datastore { get; private set; }

        [NotNull]
        internal EngineConfig EngineConfig { get; }

        /// <summary>
        ///     The queue this is
        /// </summary>
        public IJobQueue Queue { get; private set; }

        /// <summary>
        ///     True iff ready to operate.
        /// </summary>
        public bool Running => this.Queue?.Connected ?? false;

        /// <summary>
        ///     Creates a new Magnum Microservice Controller
        /// </summary>
        /// <param name="engineConfig">The configuration to use.</param>
        /// <seealso cref="Job" />
        public MagnumBiDispatchController([NotNull] EngineConfig engineConfig) {
            this.EngineConfig = engineConfig;
            this.Init();

            try {
                this.Queue.Connect();
            } catch (Exception e) {
                throw new Exception("Failed to connect to RabbitMQ", e);
            }

            Instance = this;
        }

        /// <summary>
        ///     Helpful method for setup
        /// </summary>
        private void Init() {
            // Set up queue
            switch (this.EngineConfig.QueueType) {
                case "RabbitMQ":
                    this.Queue = new RabbitJobQueue(this.EngineConfig.QueueConfig as RabbitQueueConfig);
                    break;
                default:
                    throw new Exception($"Unknown queue type {this.EngineConfig.QueueType}");
            }

            // Set up Datastore
            switch (this.EngineConfig.DatastoreType) {
                case "MongoDb":
                    try {
                        this.Datastore = new MongoDb(this.EngineConfig.DatastoreConfig as MongoDbConfig);
                    } catch (Exception e) {
                        throw new Exception("Failed to connect to MongoDB", e);
                    }

                    break;
                case "PostgreSql":
                    try {
                        this.Datastore = new PostgreSql(this.EngineConfig.DatastoreConfig as PostgreSqlConfig);
                    } catch (Exception e) {
                        throw new Exception("Failed to connect to PostgreSQL", e);
                    }

                    break;
                default:
                    throw new Exception($"Unknown datastore type {this.EngineConfig.DatastoreType}");
            }
        }

        /// <summary>
        ///     Enqueues a new Job on the job queue.
        /// </summary>
        /// <param name="appId">Queue name</param>
        /// <param name="job">ID of job to queue</param>
        public void QueueJob([NotNull] string appId, [NotNull] Job job) {
            Log.Debug("Engine: Adding new job", appId, job.JobId);
            this.Datastore.Add(appId, job);
            this.Queue.QueueJob(appId, job.JobId);
        }

        /// <summary>
        ///     Indicates that a job has failed.
        /// </summary>
        /// <param name="jobId">ID of job to fail</param>
        public void FailJob(string jobId) {
            Log.Debug("Engine: Failing job", jobId);
            this.Queue.FailJob(jobId);
        }

        /// <summary>
        ///     Returns a job if there is one waiting to be processed on the queue,
        ///     otherwise returns null.
        /// </summary>
        /// <param name="appId">Queue name</param>
        /// <returns>Job waiting to be processed, or null if none exists.</returns>
        public Job RetrieveJob(string appId) {
            Job job = null;
            Log.Debug("Engine: Getting job", appId);

            do {
                string jobId = this.Queue.RetrieveJobId(appId);
                if (jobId == null) {
                    return null;
                }

                try {
                    job = this.Datastore.Get(appId, jobId);
                } catch (Exception e) {
                    this.Queue.CompleteJob(jobId);
                    Log.Error("Could not retrieve job data",
                        e,
                        jobId,
                        appId);
                    return null;
                }
            } while (job == null);

            return job;
        }

        /// <summary>
        ///     Marks a job as completed and removes it from the datastore.
        /// </summary>
        /// <param name="appId">Queue name</param>
        /// <param name="jobId">ID of job to complete</param>
        public void CompleteJob(string appId, string jobId) {
            Log.Debug("Engine: Completing job", appId, jobId);

            this.Queue.CompleteJob(jobId);
            this.Datastore.Remove(appId, jobId);
        }

        /// <summary>
        ///     Returns true if there is a job waiting for an application, otherwise returns false.
        /// </summary>
        /// <param name="appId">Queue name</param>
        /// <returns>True iff there is a job waiting for an application.</returns>
        public bool JobWaiting(string appId) {
            return !this.Queue.IsEmpty(appId);
        }

        /// <summary>
        ///     Purges all jobs waiting for an application.
        /// </summary>
        /// <param name="appId">Queue name</param>
        public void PurgeJobs(string appId) {
            this.Queue.ResetQueue(appId);
            this.Datastore.ClearQueue(appId);
        }

        public bool IsTrackingJob(string jobId) {
            return this.Queue.IsTrackingJob(jobId);
        }

        /// <summary>
        ///     If the database supports it, ensures the uniqueness of a jobId.
        ///     ie. If the given jobId is not unique, return one which is (if possible).
        /// </summary>
        /// <returns>
        ///     If uniqueness checking is not supported, returns the given jobId.
        ///     If uniqueness checking is supported, returns the given jobId if it is unique,
        ///     else generates a new and unique jobId.
        /// </returns>
        private string CheckJobId(string jobId) {
            if (!this.Datastore.HandleJobIds) {
                return jobId;
            }

            while (!this.Datastore.IsJobIdAlreadyUsed(jobId)) {
                jobId = GenerateNewJobId();
            }

            return jobId;
        }

        /// <summary>
        ///     Generates a new Job ID which will be unique if the database supports
        ///     uniqueness checking.
        /// </summary>
        /// <returns>A Job Id which may be unique depending on the database used.</returns>
        public static string NewJobId() {
            string guid = GenerateNewJobId();
            guid = Instance?.CheckJobId(guid) ?? guid;
            return guid;
        }

        /// <summary>
        ///     Creates a new Job ID that is likely to be unique.
        /// </summary>
        /// <returns>A new Job ID</returns>
        private static string GenerateNewJobId() {
            string id = "";
            for (int i = 0; i < IdLength; i++) {
                int charIndex = RandomGen.Next(0, ValidIdChars.Length);
                id += ValidIdChars[charIndex];
            }

            return id;
        }

        /// <summary>
        ///     Shut down the engine cleanly.
        /// </summary>
        public void Shutdown() {
            try {
                this.Queue.DisconnectAndClose();
                this.Datastore.Close();
            } catch (Exception e) {
                Log.Error("Failed to shutdown cleanly", e);
            }

            Log.Debug($"Engine closed Queue: ${this.Queue.Connected}");
        }
    }
}