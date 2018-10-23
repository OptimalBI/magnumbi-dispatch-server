#region FileInfo

// MagnumBI.Dispatch MagnumBI.Dispatch.Engine RabbitJobQueue.cs
// Created: 20171016
// Edited: 20171016
// By: Timothy Gray (timgray)

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MagnumBI.Dispatch.Engine.Config.Queue;
using MagnumBI.Dispatch.Engine.Exceptions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;

namespace MagnumBI.Dispatch.Engine.Queue {
    /// <summary>
    ///     Queue representation for RabbitMQ.
    /// </summary>
    public class RabbitJobQueue : BaseJobQueue, IDisposable {
        private const string Exchange = "MagnumMicroservicesExchange";
        private const string VirtualHost = "/";
        private readonly RabbitQueueConfig config;
        private readonly ConcurrentDictionary<string, ulong> jobWaitingAck = new ConcurrentDictionary<string, ulong>();
        private readonly object locker = new object();
        private IModel channel; // Lock must be used to access, to be improved later.
        private bool connectedOnce;
        private IConnection connection; // Locking this too.
        private ConnectionFactory factory;

        /// <inheritdoc />
        public override bool Connected {
            get {
                lock (this.locker) {
                    return (this.connection?.IsOpen ?? false) && (this.channel?.IsOpen ?? false);
                }
            }
        }

        /// <summary>
        ///     Default constructor for RabbitJobQueue.
        /// </summary>
        /// <param name="config">Configurations for RabbitMQ</param>
        public RabbitJobQueue(RabbitQueueConfig config) {
            if (config.QueueType != "RabbitMQ") {
                throw new ArgumentException($"{nameof(config.QueueType)} is not of type RabbitMQ!");
            }
            this.config = config;
        }

        public void Dispose() {
            this.DisconnectAndClose();
        }

        /// <inheritdoc />
        public override void QueueJob(string qid, string jobId) {
            this.VerifyReadyToGo();

            this.CreateQueue(qid);
            byte[] messageBody = Encoding.UTF8.GetBytes(jobId);

            lock (this.locker) {
                IBasicProperties props = this.channel.CreateBasicProperties();
                props.DeliveryMode = 2; // Make sure the message survives a RabbitMQ reboot
                props.ContentType = "text/plain";
                this.channel.BasicPublish(
                    Exchange,
                    qid,
                    body: messageBody,
                    basicProperties: props
                );
            }
        }

        /// <inheritdoc />
        public override string RetrieveJobId(string qid) {
            Log.Debug("Getting job requested", qid);
            if (qid == null) {
                throw new NullReferenceException($"{nameof(qid)} cannot be null");
            }

            this.VerifyReadyToGo();
            this.CreateQueue(qid);

            BasicGetResult data;
            if (this.channel == null) {
                this.Connect();
            }
            lock (this.locker) {
                data = this.channel.BasicGet(qid, false);
                if (data == null) {
                    return null;
                }
            }
            string jobData = Encoding.UTF8.GetString(data.Body);

            int addCount = 0;
            while (!this.jobWaitingAck.TryAdd(jobData, data.DeliveryTag)) {
                // add failed - try again
                Thread.Sleep(10);
                // check for timeout
                if (addCount++ > 2) {
                    throw new JobReceptionException("Failed to add job to waiting ack list.");
                }
            }
            return jobData;
        }

        /// <inheritdoc />
        public override void CreateQueue(string qid) {
            this.VerifyReadyToGo();
            lock (this.locker) {
                QueueDeclareOk queueDeclareResponse = this.channel.QueueDeclare(
                    qid,
                    true,
                    false,
                    false
                );
                this.channel.QueueBind(queueDeclareResponse.QueueName, Exchange, qid);
            }
        }

        /// <inheritdoc />
        public override bool IsEmpty(string qid) {
            this.VerifyReadyToGo();
            this.CreateQueue(qid);
            try {
                lock (this.locker) {
                    return this.channel.MessageCount(qid) == 0;
                }
            } catch (Exception e) {
                Log.Debug($"Error checking if queue is empty assuming it is. Exception Message: {e.Message}");
                this.Connect();
                return true;
            }
        }

        /// <inheritdoc />
        public override void ResetQueue(string qid) {
            this.VerifyReadyToGo();

            try {
                lock (this.locker) {
                    this.channel.QueuePurge(qid);
                }
            } catch (Exception e) {
                Log.Debug($"Exception on queue purge {e.Message}");
                this.Connect();
            }
        }

        /// <inheritdoc />
        public override void Connect() {
            lock (this.locker) {
                if (this.Connected) {
                    return;
                }
                if (this.connectedOnce) {
                    Log.Error($"RabbitMQ reconnection triggered!");
                } else {
                    this.connectedOnce = true;
                }
                // Create connection
                if (this.connection == null || !this.connection.IsOpen) {
                    try {
                        this.factory = new ConnectionFactory {
                            HostName = this.config.Hostname,
                            Password = this.config.Password,
                            UserName = this.config.Username,
                            HandshakeContinuationTimeout = TimeSpan.FromSeconds(8),
                            Port = this.config.Port,
                            RequestedHeartbeat = 1,
                            ContinuationTimeout = TimeSpan.FromSeconds(8),
                            RequestedConnectionTimeout = (int) TimeSpan.FromSeconds(8).TotalMilliseconds,
                            VirtualHost = VirtualHost,
                            UseBackgroundThreadsForIO = true,
                            AutomaticRecoveryEnabled = true
                        };
                        if (this.connection?.IsOpen ?? false) {
                            this.connection?.Close(200);
                        }
                        this.connection = this.factory.CreateConnection();
                    } catch (Exception e) {
                        Log.Error("Failed to create RabbitMQ connection: ", e);
                        throw;
                    }
                }
                if (this.channel == null || !this.channel.IsOpen) {
                    // Create channel
                    try {
                        if (this.channel?.IsOpen ?? false) {
                            this.channel?.Close(200, "Back soon.");
                        }
                        this.channel = this.connection.CreateModel();
                        this.channel.BasicQos(0, 1000, true);
                        this.channel.ExchangeDeclare(
                            Exchange,
                            "direct",
                            true
                        );
                    } catch (Exception e) {
                        Log.Error("Failed to create RabbitMQ channel: ", e);
                        throw;
                    }
                }
            }
        }

        public override bool IsTrackingJob(string jobId) {
            return this.jobWaitingAck.ContainsKey(jobId);
        }

        /// <inheritdoc />
        public override void DisconnectAndClose() {
            lock (this.locker) {
                // Reject all waiting jobs
                foreach (KeyValuePair<string, ulong> keyValuePair in this.jobWaitingAck) {
                    this.channel.BasicNack(keyValuePair.Value, false, true);
                }
                // Close channel
                this.channel.Close(200, "Client shutdown.");
                // Close connection
                this.connection.Close(200, "Goodbye.", 2000);
            }
        }

        /// <inheritdoc />
        public override void CompleteJob(string jobId) {
            Log.Debug("Finishing job", jobId);

            this.VerifyReadyToGo();

            ulong ackId;
            int getAttempt = 0;
            while (!this.jobWaitingAck.TryGetValue(jobId, out ackId)) {
                if (getAttempt++ > 2) {
                    throw new CompleteJobException("Failed to get job ackId, did job exist?");
                }
            }
            Log.Debug($"Finishing job {jobId} with ackId {ackId}");
            lock (this.locker) {
                this.channel.BasicAck(ackId, false);
            }
#pragma warning disable 168
            int addCount = 0;
            while (!this.jobWaitingAck.TryRemove(jobId, out ulong ackIdOut)) {
                // remove failed - try again
                Thread.Sleep(10);
                // check for timeout
                if (addCount++ > 2) {
                    throw new JobReceptionException("Failed to remove job after completion");
                }
            }
#pragma warning restore 168
        }

        /// <inheritdoc />
        public override void FailJob(string jobId) {
            this.VerifyReadyToGo();

            if (!this.jobWaitingAck.ContainsKey(jobId)) {
                throw new CancelJobException("Could not find job.");
            }

            ulong ackId;
            bool success;
            int attempts = 0;
            do {
                success = this.jobWaitingAck.TryGetValue(jobId, out ackId);
                if (attempts++ > 3) {
                    throw new CancelJobException($"Failed to find job {jobId}");
                }
            } while (!success);

            Log.Debug($"Failing job {jobId} with ackId {ackId}");
            lock (this.locker) {
                this.channel.BasicNack(ackId, false, true);
            }
#pragma warning disable 168
            int addCount = 0;
            while (!this.jobWaitingAck.TryRemove(jobId, out ulong ackIdOut)) {
                // check for timeout
                if (addCount++ > 2) {
                    throw new JobReceptionException("Failed to remove job after completion");
                }
            }
#pragma warning restore 168
        }

        /// <inheritdoc />
        public override void DeleteQueue(string qid) {
            this.VerifyReadyToGo();
            lock (this.locker) {
                uint deleteResponse = this.channel.QueueDelete(qid, false, false);
                Log.Debug($"Deleted queue {qid} which contained {deleteResponse} messages.");
            }
        }

        /// <inheritdoc />
        public override bool QueueExists(string qid) {
            return this.Queues()?.Contains(qid) ?? false;
        }

        /// <summary>
        ///     Creates a list of all queues.
        /// </summary>
        /// <returns>A list containing the ID of each queue.</returns>
        public List<string> Queues() {
            Log.Debug("Listing queues.");

            if (this.config.ManagementPort == -1) {
                // In this case, operation is not supported
                return null;
            }
            HttpClientHandler handler = new HttpClientHandler {
                SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };
            HttpClient client = new HttpClient(handler);
            string passString = $"{this.config.Username}:{this.config.Password}";
            byte[] passByts = Encoding.Default.GetBytes(passString);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(passByts));

            Task<HttpResponseMessage> getTask =
                client.GetAsync(new Uri($"http://{this.config.Hostname}:{this.config.ManagementPort}/api/bindings"));
            getTask.Wait(TimeSpan.FromSeconds(30));
            HttpResponseMessage response = getTask.Result;
            if (!response.IsSuccessStatusCode) {
                throw new Exception("Failed to query RabbitMQ management layer.");
            }
            Task<string> responseStringTask = response.Content.ReadAsStringAsync();
            responseStringTask.Wait();
            string responseString = responseStringTask.Result;
            File.WriteAllText("JsonOutput.json", responseString);
            List<string> queueNames = new List<string>();
            List<Dictionary<string, object>> content =
                JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseString);
            foreach (Dictionary<string, object> dictionary in content) {
                if ((string) dictionary["source"] == Exchange) {
                    queueNames.Add((string) dictionary["destination"]);
                }
            }

            return queueNames;
        }
    }
}