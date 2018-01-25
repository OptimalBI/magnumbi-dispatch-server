using MagnumBI.Dispatch.Engine.Config.Datastore;
using MagnumBI.Dispatch.Engine.Config.Queue;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config {
    /// <summary>
    ///     Config for this system's engine.
    /// </summary>
    public class EngineConfig {
        /// <summary>
        ///     Config for the datastore
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto, Required = Required.Always)]
        public BaseDatastoreConfig DatastoreConfig;

        /// <summary>
        ///     Config for the queue
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto, Required = Required.Always)]
        public BaseQueueConfig QueueConfig;

        /// <summary>
        ///     The amount of time to wait while attempting to connect.
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public int TimeoutSeconds = 12;

        /// <summary>
        ///     Type of datastore to use
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DatastoreType => this.DatastoreConfig.DatastoreType;

        /// <summary>
        ///     Type of queue to use
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string QueueType => this.QueueConfig.QueueType;

        /// <summary>
        ///     Creates a new EngineConfig from the given JSON.
        /// </summary>
        /// <param name="json">JSON to create config from</param>
        /// <returns>The EngineConfig equivalent of the given JSON.</returns>
        public static EngineConfig FromJson(string json) {
            return EngineConfigHelper.FromJson(json);
        }

        /// <summary>
        ///     Determines whether this EngineConfig is equal to another.
        /// </summary>
        /// <param name="other">The EngineConfig to compare this with</param>
        /// <returns>True iff the this is equal to other.</returns>
        protected bool Equals(EngineConfig other) {
            return Equals(this.DatastoreConfig, other.DatastoreConfig);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return this.Equals((EngineConfig) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.DatastoreConfig != null ? this.DatastoreConfig.GetHashCode() : 0;
        }

        /// <inheritdoc />
        public override string ToString() {
            return
                $"{nameof(this.DatastoreConfig)}: {this.DatastoreConfig}," +
                $"{nameof(this.QueueConfig)}: {this.QueueConfig}," +
                $"{nameof(this.TimeoutSeconds)}: {this.TimeoutSeconds}," +
                $"{nameof(this.DatastoreType)}: {this.DatastoreType}," +
                $"{nameof(this.QueueType)}: {this.QueueType}";
        }
    }
}