using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Datastore;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MagnumBI.Dispatch.Tests {
    public class MongoDbTests : IDisposable {
        public MongoDbTests(ITestOutputHelper output) {
            this.output = output;

            LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose);
            Log.Logger = logConfig.CreateLogger();

            // Setup mongo
            string text = File.ReadAllText(this.configFile);
            this.engineConfig = JsonConvert.DeserializeObject<EngineConfig>(text);
            MongoDbConfig mongoDbConfig = this.engineConfig.DatastoreConfig as MongoDbConfig;
            this.mongoDb = new MongoDb(mongoDbConfig);
            this.mongoDb.ClearData();
        }

        public void Dispose() {
            string text = File.ReadAllText(this.configFile);
            this.engineConfig = JsonConvert.DeserializeObject<EngineConfig>(text);
            this.mongoDb.ClearData();
        }

        private readonly string logFile =
            Path.Combine(AppContext.BaseDirectory, "Optimal.MagnumMicroservices.Tests.log");

        private readonly string configFile = Path.Combine(AppContext.BaseDirectory, "TestMongodbConfig.json");

        private const string Qid = "test1";

        private EngineConfig engineConfig;
        private readonly IDatastore mongoDb;
        private ITestOutputHelper output;

        [Fact]
        public void TestMongoAdd1() {
//            dynamic eo = new ExpandoObject();
//            eo.TestData = "Data";
            Dictionary<string, string> dict = new Dictionary<string, string> {
                {
                    "hi", "tester"
                }
            };
            this.mongoDb.Add(Qid, new Job(dict, null, null));
        }

        [Fact]
        public void TestMongoAdd2() {
            for (int i = 0; i < 6; i++) {
                dynamic eo = new ExpandoObject();
                eo.TestData = "Data" + i;
                this.mongoDb.Add(Qid, new Job(eo));
            }
        }

        [Fact]
        public void TestMongoAdd3() {
            for (int i = 0; i < 60; i++) {
                dynamic eo = new ExpandoObject();
                eo.TestData = "Data" + i;
                this.mongoDb.Add(Qid, new Job(eo));
            }
        }

        [Fact]
        public void TestMongoClear() {
            dynamic eo = new ExpandoObject();
            eo.TestData = "Data";

            this.mongoDb.Add(Qid, new Job(eo));
            this.mongoDb.ClearQueue(Qid);
            Assert.True(this.mongoDb.IsEmpty(Qid));
        }

        [Fact]
        public void TestMongoDelete1() {
            dynamic eo = new ExpandoObject();
            eo.TestData = "Data";

            this.mongoDb.Add(Qid, new Job(eo, jobId: "STATICJOBID"));
            this.mongoDb.Remove(Qid, "STATICJOBID");
        }

        [Fact]
        public void TestMongoGet1() {
            string jobId = MagnumBiDispatchController.NewJobId();
            Dictionary<string, string> data = new Dictionary<string, string> {
                {
                    "Test", "data"
                }
            };
            dynamic eo = new ExpandoObject();

            this.mongoDb.Add(Qid, new Job(data, jobId));

            Job jobReturned = this.mongoDb.Get(Qid, jobId);
            Assert.Equal(jobReturned.JobId, jobId);
            Assert.Equal(jobReturned.Data, data);
        }

        [Fact]
        public void TestMongoGet2() {
            // Add jobs
            List<Job> jobs = new List<Job>();
            for (int i = 0; i < 120; i++) {
                Job j = new Job(new Dictionary<string, string> {
                    {
                        "Data", MagnumBiDispatchController.NewJobId()
                    }
                });
                jobs.Add(j);
                this.mongoDb.Add(Qid, j);
            }

            // Check jobs
            Job previousJob = null;
            foreach (Job job in jobs) {
                Job returnedJob = this.mongoDb.Get(Qid, job.JobId);
                Assert.Equal(returnedJob.JobId, job.JobId);
                Assert.Equal(returnedJob.Data, job.Data);
                if (previousJob != null) {
                    Assert.NotEqual(returnedJob.JobId, previousJob.JobId);
                }
                previousJob = job;
            }
        }
    }
}