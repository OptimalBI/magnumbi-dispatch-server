// 
// 0918
// 2017091812:37 PM

using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Web.Config {
    /// <summary>
    ///     Helper class for WebConfig.
    /// </summary>
    public static class WebConfigHelper {
        /// <summary>
        ///     Converts a WebConfig to JSON format.
        /// </summary>
        /// <param name="engineConfig">The config to convert</param>
        /// <returns>The JSON equivalent of engineConfig.</returns>
        public static string ToJson(this WebConfig engineConfig) {
            return JsonConvert.SerializeObject(engineConfig,
                Formatting.Indented,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
        }

        /// <summary>
        ///     Converts a JSON string to WebConfig format.
        /// </summary>
        /// <param name="json">The JSON to convert</param>
        /// <returns>The WebConfig equivalent of json.</returns>
        public static WebConfig FromJson(string json) {
            return JsonConvert.DeserializeObject<WebConfig>(json,
                new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto
                });
        }
    }
}