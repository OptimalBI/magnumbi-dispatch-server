// 
// 0918
// 2017091812:37 PM

using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Engine.Config;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Web.Config {
    /// <summary>
    ///     Config for web module.
    /// </summary>
    [JsonObject(ItemRequired = Required.AllowNull)]
    public class WebConfig {
        /// <summary>
        ///     The access key to use AWS services.
        /// </summary>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AwsAccessKey;

        /// <summary>
        ///     The region to log to in CloudWatch (if enabled).
        /// </summary>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AwsLogRegion;

        /// <summary>
        ///     The secret key to access AWS services.
        /// </summary>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AwsSecretKey;

        /// <summary>
        ///     The configuration for the core MagnumBI Dispatch engine.
        ///     <see cref="MagnumBiDispatchController" />
        ///     <seealso cref="Engine.Config.EngineConfig" />
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public EngineConfig EngineConfig { get; set; }

        /// <summary>
        ///     The minimum level for a log event to be logged.
        /// </summary>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LogLevel { get; set; } = "Debug";

        /// <summary>
        ///     Should we write log events to file?
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool LogToFile { get; set; } = true;

        /// <summary>
        ///     The access port for MagnumBI Dispatch's web interface.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int Port { get; set; } = 6883;

        /// <summary>
        ///     Do we required clients to authenticate (recommended)
        ///     <seealso cref="AuthenticationMiddleware" />
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool UseAuth { get; set; } = true;

        /// <summary>
        ///     Should we log to AWS CloudWatch logs?
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool UseCloudWatchLogging { get; set; } = true;

        #region SSL Config

        /// <summary>
        ///     Should the MagnumBI Dispatch server use
        /// </summary>
        [JsonProperty(Required = Required.Always, Order = 99)]
        public bool UseSsl { get; set; } = true;

        /// <summary>
        ///     Ssl certificate to use.
        /// </summary>
        [JsonProperty(Required = Required.Always, DefaultValueHandling = DefaultValueHandling.Include, Order = 99)]
        public string SslCertLocation = "Cert.pfx";

        /// <summary>
        ///     Password to the Ssl certificate.
        /// </summary>
        [JsonProperty(Required = Required.Always, DefaultValueHandling = DefaultValueHandling.Populate, Order = 99)]
        public string SslCertPassword = "CERT-PASSWORD";

        #endregion
    }
}