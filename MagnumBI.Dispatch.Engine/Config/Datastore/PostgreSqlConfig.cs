using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config.Datastore {
    /// <summary>
    ///     Config for PostgreSQL datastore.
    /// </summary>
    public class PostgreSqlConfig : BaseDatastoreConfig {
        private static readonly string[] RequiredConfigValues = {
            "PostgreSqlHostnames", "PostgreSqlAdminDb", "PostgreSqlPassword", "PostgreSqlUser"
        };

        /// <summary>
        ///     Admin database
        /// </summary>
        public string PostgreSqlAdminDb { get; set; }

        /// <summary>
        ///     Database to use
        /// </summary>
        public string PostgreSqlDb { get; set; }

        /// <summary>
        ///     Hostnames for PostgreSQL, if not using a replica set only the first will be used.
        /// </summary>
        public string[] PostgreSqlHostnames { get; set; } = {
            "127.0.0.1:27017"
        };

        /// <summary>
        ///     Password for PostgreSQL
        /// </summary>
        public string PostgreSqlPassword { get; set; }

        /// <summary>
        ///     Username for PostgreSQL
        /// </summary>
        public string PostgreSqlUser { get; set; }

        #region Optional

        /// <inheritdoc />
        [JsonProperty(Order = 999)]
        public MongoDbSslConfig SslConfig { get; set; }

        #endregion

        /// <summary>
        ///     Default constructor for PostgreSqlConfig.
        /// </summary>
        public PostgreSqlConfig() : base("PostgreSql") {
        }

        /// <summary>
        ///     Constructor for MongoDbConfig.
        /// </summary>
        /// <param name="configValues">
        ///     Config settings and their corresponding values.
        /// </param>
        public PostgreSqlConfig([NotNull] Dictionary<string, string> configValues) : base("PostrgeSql") {
            bool containsAll = RequiredConfigValues.All(configValues.ContainsKey);
            if (!containsAll) {
                throw new ArgumentException(
                    $"{nameof(configValues)} did not contain all of the required configuration values. " +
                    $"The required values are: {RequiredConfigValues}");
            }
            this.PostgreSqlUser = configValues["PostgreSqlUser"];
            this.PostgreSqlPassword = configValues["PostgreSqlPassword"];
            this.PostgreSqlDb = configValues["PostgreSqlDb"];
        }

        public override Type GetDatastoreConfigType() {
            return typeof(PostgreSqlConfig);
        }
    }

    /// <summary>
    ///     Config for SSL
    /// </summary>
    public class PostgreSqlSslConfig {
        /// <summary>
        ///     List of client certificates to verify ssl.
        /// </summary>
        public List<KeyValuePair<string, string>> ClientCertificates { get; set; }

        /// <summary>
        ///     True iff Postgres should use SSL
        /// </summary>
        public bool UseSsl { get; set; } = true;

        /// <summary>
        ///     Should we attempt to verify the ssl certificate of the server.
        /// </summary>
        public bool VerifySsl { get; set; } = true;
    }
}