using System;

namespace MagnumBI.Dispatch.Engine.Config.Queue {
    /// <summary>
    ///     Interface for queue config classes.
    /// </summary>
    public interface IQueueConfig {
        /// <summary>
        ///     The queue service used.
        /// </summary>
        string QueueType { get; }

        /// <summary>
        ///     Gets the type of queue config.
        /// </summary>
        /// <returns>The type of queue config used.</returns>
        Type GetQueueConfigType();
    }
}