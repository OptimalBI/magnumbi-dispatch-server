using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MagnumBI.Dispatch.Tests {
    public class EngineTests : IDisposable {
        public EngineTests(ITestOutputHelper output) {
            this.output = output;

            LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose);
            Log.Logger = logConfig.CreateLogger();

            // Application stuff.
            this.testAppId = "TESTAPPLICATION" +
                             DateTime.UtcNow.ToString("u")
                                 .Replace(" ", "")
                                 .Replace("-", "")
                                 .Replace(":", "");

            if (!File.Exists(this.configFile)) {
                using (StreamWriter fileStream = File.CreateText(this.configFile)) {
                    EngineConfig cfg = new EngineConfig {
                        DatastoreConfig = new MongoDbConfig {
                            MongoAuthDb = "admin",
                            MongoHostnames = new[] {
                                "127.0.0.1:27017"
                            },
                            MongoUser = "Username",
                            MongoPassword = "Password",
                            MongoCollection = "MagnumMicroservices"
                        },
                        QueueConfig = new RabbitQueueConfig {
                            Hostname = "127.0.0.1",
                            Password = "Password",
                            Username = "Username"
                        },
                        TimeoutSeconds = 8
                    };
                    fileStream.Write(cfg.ToJson());
                }
                throw new Exception("Config file did not exist.");
            }

            string fileText = File.ReadAllText(this.configFile);
            this.engineConfig = EngineConfig.FromJson(fileText);
            this.engine = new MagnumBiDispatchController(this.engineConfig);
        }

        public void Dispose() {
            this.engine.Queue.Connect();
            this.engine.PurgeJobs(this.testAppId);
            this.engine.Queue.DeleteQueue(this.testAppId);
            this.engine.Shutdown();

            Log.Verbose($"Tests disposed.");
        }

        private readonly string configFile = Path.Combine(AppContext.BaseDirectory, "TestMongoDbConfig.json");
        private readonly ITestOutputHelper output;
        private readonly MagnumBiDispatchController engine;
        private readonly string testAppId;
        private readonly EngineConfig engineConfig;

        [Fact]
        public void QuickJobsTests() {
            // Add jobs
            List<string> jobs = new List<string>();
            for (int i = 0; i < 256; i++) {
                Job j = new Job(new Dictionary<string, string> {
                    {
                        "Data", Guid.NewGuid().ToString("N")
                    }
                });
                jobs.Add(j.JobId);
                this.engine.QueueJob(this.testAppId, j);
            }

            // Get jobs
            int getCount = 0;
            while (jobs.Count > 0) {
                getCount++;
                Job j = this.engine.RetrieveJob(this.testAppId);
                Assert.False(j == null);
                Assert.Contains(j.JobId, jobs);
                jobs.Remove(j.JobId);
                this.engine.CompleteJob(this.testAppId, j.JobId);
            }
            Assert.Empty(jobs);
            Assert.False(this.engine.JobWaiting(this.testAppId));
        }

        [Fact]
        public void TestConfig() {
            Assert.False(string.IsNullOrWhiteSpace(this.engineConfig.DatastoreType));
            Assert.False(string.IsNullOrWhiteSpace(this.engineConfig.QueueType));
        }

        [Fact]
        public void TestJobProcess1() {
            // Check test queue is empty
            if (this.engine.JobWaiting(this.testAppId)) {
                this.engine.PurgeJobs(this.testAppId);
            }

            // Add outgoing job.
            Job j = new Job(new Dictionary<string, string> {
                {
                    "Test", "data"
                }
            });

            this.engine.QueueJob(this.testAppId, j);

            Thread.Sleep(100);

            // Check job is on queue
            Assert.True(this.engine.JobWaiting(this.testAppId));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            // Get job back
            Job retrJob = this.engine.RetrieveJob(this.testAppId);
            Assert.False(retrJob == null);

            Assert.Equal(j, retrJob);

            this.engine.CompleteJob(this.testAppId, retrJob.JobId);

            // Check queue is empty
            Assert.False(this.engine.JobWaiting(this.testAppId));

            this.engine.Shutdown();
        }
    }
}