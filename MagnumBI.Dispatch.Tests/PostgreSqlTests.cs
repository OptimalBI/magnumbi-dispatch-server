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
    public class PostgreSqlTests : IDisposable {
        public PostgreSqlTests(ITestOutputHelper output) {
            this.output = output;
            LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose);
            Log.Logger = logConfig.CreateLogger();

            // Setup db
            string text = File.ReadAllText(this.configFile);
            this.engineConfig = JsonConvert.DeserializeObject<EngineConfig>(text);
            PostgreSqlConfig config = this.engineConfig.DatastoreConfig as PostgreSqlConfig;
            this.datastore = new PostgreSql(config);
            this.datastore.ClearData();
        }

        public void Dispose() {
            string text = File.ReadAllText(this.configFile);
            this.engineConfig = JsonConvert.DeserializeObject<EngineConfig>(text);
            this.datastore.ClearData();
        }

        private readonly string logFile =
            Path.Combine(AppContext.BaseDirectory, "Optimal.MagnumMicroservices.Tests.log");

        private readonly string configFile = Path.Combine(AppContext.BaseDirectory, "TestPostrgesConfig.json");

        private const string Qid = "test1";

        private EngineConfig engineConfig;
        private readonly IDatastore datastore;
        private ITestOutputHelper output;

        [Fact]
        public void TestAdd1() {
            //            dynamic eo = new ExpandoObject();
            //            eo.TestData = "Data";
            Dictionary<string, string> dict = new Dictionary<string, string> {
                {
                    "hi", "tester"
                }
            };
            this.datastore.Add(Qid, new Job(dict, null, null));
        }

        [Fact]
        public void TestAdd2() {
            for (int i = 0; i < 6; i++) {
                dynamic eo = new ExpandoObject();
                eo.TestData = "Data" + i;
                this.datastore.Add(Qid, new Job(eo));
            }
        }

        [Fact]
        public void TestAdd3() {
            for (int i = 0; i < 60; i++) {
                dynamic eo = new ExpandoObject();
                eo.TestData = "Data" + i;
                this.datastore.Add(Qid, new Job(eo));
            }
        }

        [Fact]
        public void TestClear() {
            dynamic eo = new ExpandoObject();
            eo.TestData = "Data";

            this.datastore.Add(Qid, new Job(eo));
            this.datastore.ClearQueue(Qid);
            Assert.True(this.datastore.IsEmpty(Qid));
        }

        [Fact]
        public void TestDelete1() {
            dynamic eo = new ExpandoObject();
            eo.TestData = "Data";

            this.datastore.Add(Qid, new Job(eo, jobId: "STATICJOBID"));
            this.datastore.Remove(Qid, "STATICJOBID");
        }

        [Fact]
        public void TestGet1() {
            string jobId = MagnumBiDispatchController.NewJobId();
            Dictionary<string, string> data = new Dictionary<string, string> {
                {
                    "Test", "data"
                }
            };

            this.datastore.Add(Qid, new Job(data, jobId));

            Job jobReturned = this.datastore.Get(Qid, jobId);
            Dictionary<string, string> dataReturned = jobReturned.Data.ToObject<Dictionary<string, string>>();
            Assert.Equal(jobReturned.JobId, jobId);
            Assert.Equal(dataReturned, data);
        }

        [Fact]
        public void TestGet2() {
            // Add jobs
            List<Job> jobs = new List<Job>();
            for (int i = 0; i < 120; i++) {
                Job j = new Job(new Dictionary<string, string> {
                    {
                        "Data", MagnumBiDispatchController.NewJobId()
                    }
                });
                jobs.Add(j);
                this.datastore.Add(Qid, j);
            }

            // Check jobs
            Job previousJob = null;
            foreach (Job job in jobs) {
                Job jobReturned = this.datastore.Get(Qid, job.JobId);
                Dictionary<string, string> dataReturned = jobReturned.Data.ToObject<Dictionary<string, string>>();
                Assert.Equal(jobReturned.JobId, job.JobId);
                Assert.Equal(dataReturned, job.Data);
                if (previousJob != null) {
                    Assert.NotEqual(jobReturned.JobId, previousJob.JobId);
                }
                previousJob = job;
            }
        }
    }
}