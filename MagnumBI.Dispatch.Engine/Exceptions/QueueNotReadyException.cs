using System;

namespace MagnumBI.Dispatch.Engine.Exceptions {
    /// <summary>
    ///     Exception for throwing when an operation has been attempted
    ///     on a queue which was not ready.
    /// </summary>
    public class QueueNotReadyException : Exception {
        public QueueNotReadyException() {
        }

        public QueueNotReadyException(string message) : base(message) {
        }
    }
}