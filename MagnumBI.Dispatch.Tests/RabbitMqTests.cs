using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Engine.Config;
using MagnumBI.Dispatch.Engine.Config.Queue;
using MagnumBI.Dispatch.Engine.Queue;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;

namespace MagnumBI.Dispatch.Tests {
    public class RabbitMqTests : IDisposable {
        private readonly string configFile = Path.Combine(AppContext.BaseDirectory, "TestMongodbConfig.json");

        public RabbitMqTests(ITestOutputHelper output) {
            LoggerConfiguration logConfig = new LoggerConfiguration().WriteTo.Console()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose);
            Log.Logger = logConfig.CreateLogger();

            this.engineConfig = JsonConvert.DeserializeObject<EngineConfig>(File.ReadAllText(this.configFile));
            Assert.True(this.engineConfig.QueueType == "RabbitMQ");
            this.queueConfig = this.engineConfig.QueueConfig as RabbitQueueConfig;

            this.rabbitJobQueue = new RabbitJobQueue(this.queueConfig);
            this.rabbitJobQueue.Connect();
        }

        public void Dispose() {
            // Remove test queues
            if (CleanUpQueues) {
                foreach (string testQueue in this.testQueues) {
                    RabbitJobQueue jobQueue = this.rabbitJobQueue;
                    jobQueue?.DeleteQueue(testQueue);
                }
            }

            if (!this.rabbitJobQueue?.Connected ?? true) {
                this.rabbitJobQueue?.DisconnectAndClose();
            }
        }

#if DEBUG
        private const bool CleanUpQueues = true;
#else
        private const bool CleanUpQueues = true;
#endif
        private static readonly Random random = new Random();
        private readonly RabbitJobQueue rabbitJobQueue;
        private readonly EngineConfig engineConfig;
        private readonly RabbitQueueConfig queueConfig;

        private readonly string[] testQueues = {
            "test1", "test2", "test3"
        };

        private string RandomQueue() {
            return this.testQueues[random.Next(0, this.testQueues.Length)];
        }

        [Fact]
        public void CheckConfig() {
            Assert.True(!string.IsNullOrWhiteSpace(this.queueConfig.Hostname));
            Assert.True(!string.IsNullOrWhiteSpace(this.queueConfig.Password));
            Assert.True(!string.IsNullOrWhiteSpace(this.queueConfig.Username));

            Assert.NotNull(this.rabbitJobQueue);
            Assert.True(this.rabbitJobQueue.Connected);
        }

        [Fact]
        public void TestBasicAdd() {
            string qid = this.RandomQueue();

            // Verify queue is empty before running this.
            if (!this.rabbitJobQueue.IsEmpty(qid)) {
                this.rabbitJobQueue.ResetQueue(qid);
            }

            string jobId = MagnumBiDispatchController.NewJobId();
            this.rabbitJobQueue.QueueJob(qid, jobId);
            string returnedId = this.rabbitJobQueue.RetrieveJobId(qid);
            Assert.Equal(jobId, returnedId);
        }

        [Fact]
        public void TestBasicJobThrough() {
            string qid = this.RandomQueue();
            // Verify queue is empty before running this.
            if (!this.rabbitJobQueue.IsEmpty(qid)) {
                this.rabbitJobQueue.ResetQueue(qid);
            }

            string jobId = MagnumBiDispatchController.NewJobId();
            this.rabbitJobQueue.QueueJob(qid, jobId);
            string returnedId = this.rabbitJobQueue.RetrieveJobId(qid);
            Assert.Equal(jobId, returnedId);

            // Complete job.
            this.rabbitJobQueue.CompleteJob(jobId);
            Assert.True(this.rabbitJobQueue.IsEmpty(qid));
        }

        [Fact]
        public void TestBasicDeleteQueue() {
            if (this.queueConfig.ManagementPort == -1) {
                Log.Warning("Cannot run test due to rabbitMQ config.");
                return;
            }

            string q = this.RandomQueue();
            this.rabbitJobQueue.CreateQueue(q);
            Assert.True(this.rabbitJobQueue.QueueExists(q));
            this.rabbitJobQueue.DeleteQueue(q);
            Assert.False(this.rabbitJobQueue.QueueExists(q));
        }

        [Fact]
        public void DeleteAllQueues() {
            List<string> queues = this.rabbitJobQueue.Queues();
            foreach (string queue in queues) {
                if (!queue.StartsWith("TEST")) {
                    continue;
                }
                this.rabbitJobQueue.DeleteQueue(queue);
            }
        }

        /// <summary>
        ///     Will attempt to check if the rabbitMQ section is working for concurrent processes.
        /// </summary>
        [Fact]
        public void AttemptTestConcurrencyOne() {
            int numberOfJobsPerThread = 200;

            List<Task> tasks = new List<Task>();
            int numberOfThreadsWanted = Environment.ProcessorCount;
            if (numberOfThreadsWanted < 4) {
                numberOfThreadsWanted = 4;
            }

            foreach (string queue in this.testQueues) {
                if (!this.rabbitJobQueue.IsEmpty(queue)) {
                    this.rabbitJobQueue.DeleteQueue(queue);
                }
            }

            int expectedNumberOfJobs = numberOfThreadsWanted * numberOfJobsPerThread;

            for (int i = 0; i < numberOfThreadsWanted; i++) {
                tasks.Add(new Task(() => {
                    Thread.Sleep(100);
                    string q = this.RandomQueue();

                    for (int j = 0; j < numberOfJobsPerThread; j++) {
                        string jobId = MagnumBiDispatchController.NewJobId();
                        this.rabbitJobQueue.QueueJob(q, jobId);
                        Thread.Sleep(10);
                    }
                }));
            }

            tasks.ForEach(thread => thread.Start());
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(2));
            foreach (Task task in tasks) {
                Assert.True(task.IsCompleted, "Tasks did not complete.");
            }
            Log.Information($"Expected {expectedNumberOfJobs} jobs.");
        }

        [Fact]
        public void TestCreateQueue() {
            string q = this.RandomQueue();

            // Check q does not already exist
            if (this.rabbitJobQueue.QueueExists(q)) {
                this.rabbitJobQueue.DeleteQueue(q);
            }

            this.rabbitJobQueue.CreateQueue(q);
            Assert.True(this.rabbitJobQueue.QueueExists(q));
        }
    }
}