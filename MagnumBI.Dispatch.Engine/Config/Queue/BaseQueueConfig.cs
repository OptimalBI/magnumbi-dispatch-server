using System;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config.Queue {
    /// <summary>
    ///     Base class for queue config classes.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseQueueConfig : IQueueConfig {
        /// <summary>
        ///     Sets the queue service used by this config.
        /// </summary>
        /// <param name="queueType">Queue service used</param>
        protected BaseQueueConfig(string queueType) {
            this.QueueType = queueType;
        }

        /// <inheritdoc />
        [JsonIgnore]
        public string QueueType { get; }

        /// <inheritdoc />
        public abstract Type GetQueueConfigType();
    }
}