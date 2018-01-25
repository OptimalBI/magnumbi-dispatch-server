using System;

namespace MagnumBI.Dispatch.Engine.Config.Datastore {
    /// <summary>
    ///     Interface for datastore config classes.
    /// </summary>
    internal interface IDatastoreConfig {
        /// <summary>
        ///     The datastore service used.
        /// </summary>
        string DatastoreType { get; }

        /// <summary>
        ///     Gets the type of datastore config.
        /// </summary>
        /// <returns>The type of datastore config used.</returns>
        Type GetDatastoreConfigType();
    }
}