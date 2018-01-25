using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config.Datastore {
    /// <summary>
    ///     Config for MongoDB datastore.
    /// </summary>
    public class MongoDbConfig : BaseDatastoreConfig {
        private static readonly string[] RequiredConfigValues = {
            "MongoUser", "MongoPassword", "MongoIp", "MongoPort",
            "MongoAuthDb", "MongoCollection"
        };

        /// <summary>
        ///     Auth database to use
        /// </summary>
        public string MongoAuthDb { get; set; }

        /// <summary>
        ///     Collection to use
        /// </summary>
        public string MongoCollection { get; set; }

        /// <summary>
        ///     Hostnames for MongoDB, if not using a replica set only the first will be used.
        /// </summary>
        public string[] MongoHostnames { get; set; } = {
            "127.0.0.1:27017"
        };

        /// <summary>
        ///     Password for MongoDB
        /// </summary>
        public string MongoPassword { get; set; }

        /// <summary>
        ///     Username for MongoDB
        /// </summary>
        public string MongoUser { get; set; }

        /// <summary>
        ///     If we are connecting to a replica set, what is it's name?
        /// </summary>
        public string ReplicaSetName { get; set; }

        #region Optional

        /// <inheritdoc />
        [JsonProperty(Order = 999)]
        public MongoDbSslConfig SslConfig { get; set; }

        #endregion

        /// <summary>
        ///     Are we connecting to a replica set?
        /// </summary>
        public bool UseReplicaSet { get; set; } = false;

        /// <summary>
        ///     Default constructor for MongoDbConfig.
        /// </summary>
        public MongoDbConfig() : base("MongoDb") {
        }

        /// <summary>
        ///     Constructor for MongoDbConfig.
        /// </summary>
        /// <param name="configValues">
        ///     Config settings and their corresponding values.
        ///     Valid config settings are:
        ///     <list type="bullet">
        ///         <item>MongoUser,</item>
        ///         <item>MongoPassword,</item>
        ///         <item>MongoIp,</item>
        ///         <item>MongoPort,</item>
        ///         <item>MongoAuthDb,</item>
        ///         <item>MongoCollection</item>
        ///     </list>
        /// </param>
        public MongoDbConfig([NotNull] Dictionary<string, string> configValues) : base("MongoDb") {
            // Check all required values are given
            bool containsAll = RequiredConfigValues.All(configValues.ContainsKey);
            if (!containsAll) {
                throw new ArgumentException(
                    $"{nameof(configValues)} did not contain all of the required configuration values. " +
                    $"The required values are: {RequiredConfigValues}");
            }

            this.MongoUser = configValues["MongoUser"];
            this.MongoPassword = configValues["MongoPassword"];
            this.MongoAuthDb = configValues["MongoAuthDb"];
            this.MongoCollection = configValues["MongoCollection"];
        }

        public override Type GetDatastoreConfigType() {
            return typeof(MongoDbConfig);
        }

        public override string ToString() {
            return
                $"{nameof(this.MongoAuthDb)}: {this.MongoAuthDb}, {nameof(this.MongoCollection)}: {this.MongoCollection}, " +
                $"{nameof(this.MongoHostnames)}: {this.MongoHostnames}, {nameof(this.MongoUser)}: {this.MongoUser}, " +
                $"{nameof(this.SslConfig)}: {this.SslConfig}, {nameof(this.UseReplicaSet)}: {this.UseReplicaSet}";
        }
    }

    /// <summary>
    ///     Config for SSL
    /// </summary>
    public class MongoDbSslConfig {
        /// <summary>
        ///     List of client ssl certificates.
        /// </summary>
        public List<KeyValuePair<string, string>> ClientCertificates { get; set; }

        /// <summary>
        ///     True iff Mongo should use SSL
        /// </summary>
        public bool UseSsl { get; set; } = true;

        /// <summary>
        ///     Should the mongodb connection verify the ssl certificate?
        /// </summary>
        public bool VerifySsl { get; set; } = true;
    }
}