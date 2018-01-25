using System;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine.Config.Datastore {
    /// <summary>
    ///     Base class for datastore config classes.
    /// </summary>
    public abstract class BaseDatastoreConfig : IDatastoreConfig {
        /// <summary>
        ///     Default constructor for BaseDatastoreConfig.
        /// </summary>
        /// <param name="datastoreType">Type of database used</param>
        protected BaseDatastoreConfig(string datastoreType) {
            this.DatastoreType = datastoreType;
        }

        /// <inheritdoc />
        [JsonIgnore]
        public string DatastoreType { get; }

        /// <inheritdoc />
        public abstract Type GetDatastoreConfigType();
    }
}