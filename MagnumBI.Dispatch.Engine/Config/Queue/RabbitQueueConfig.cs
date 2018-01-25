using System;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config.Queue {
    /// <summary>
    ///     Config for RabbitMQ queues.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class RabbitQueueConfig : BaseQueueConfig {
        /// <summary>
        ///     RabbitMQ host name.
        /// </summary>
        [JsonProperty]
        public string Hostname { get; set; }

        /// <summary>
        ///     RabbitMQ management port.
        /// </summary>
        [JsonProperty]
        public int ManagementPort { get; set; } = 15672;

        /// <summary>
        ///     RabbitMQ password.
        /// </summary>
        [JsonProperty]
        public string Password { get; set; }

        /// <summary>
        ///     RabbitMQ port.
        /// </summary>
        [JsonProperty]
        public int Port { get; set; } = 5672;

        /// <summary>
        ///     RabbitMQ username.
        /// </summary>
        [JsonProperty]
        public string Username { get; set; }

        public RabbitQueueConfig() : base("RabbitMQ") {
        }

        /// <inheritdoc />
        public override string ToString() {
            return
                $"{nameof(this.Hostname)}: {this.Hostname}, {nameof(this.Username)}: {this.Username}, {nameof(this.ManagementPort)}: {this.ManagementPort}, {nameof(this.Port)}: {this.Port}";
        }

        /// <inheritdoc />
        public override Type GetQueueConfigType() {
            return typeof(RabbitQueueConfig);
        }
    }
}