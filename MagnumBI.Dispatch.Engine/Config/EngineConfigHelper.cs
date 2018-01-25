using System.Collections.Generic;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config {
    /// <summary>
    ///     Helper methods and info for the EngineConfig.
    /// </summary>
    public static class EngineConfigHelper {
        /// <summary>
        ///     Databases supported by this system.
        /// </summary>
        public static List<string> SupportedDatabaseTypes = new List<string> {
            "DynamoDb",
            "MongoDb"
        };

        /// <summary>
        ///     Queues supported by this system.
        /// </summary>
        public static List<string> SupportedQueueTypes = new List<string> {
            "RabbitMq",
            "AmazonSqs"
        };

        /// <summary>
        ///     Converts an EngineConfig to JSON format.
        /// </summary>
        /// <param name="engineConfig">Config to convert</param>
        /// <returns>The JSON equivalent of the given config</returns>
        public static string ToJson(this EngineConfig engineConfig) {
            return JsonConvert.SerializeObject(engineConfig,
                Formatting.Indented,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
        }

        /// <summary>
        ///     Converts the given JSON to an EngineConfig.
        /// </summary>
        /// <param name="json">The JSON to convert</param>
        /// <returns>The EngineConfig equivalent of the given JSON</returns>
        public static EngineConfig FromJson(string json) {
            return JsonConvert.DeserializeObject<EngineConfig>(json,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
        }
    }
}