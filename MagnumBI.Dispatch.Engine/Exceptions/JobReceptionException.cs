using System;

namespace MagnumBI.Dispatch.Engine.Exceptions {
    /// <summary>
    ///     Exception thrown when receiving a job fails.
    /// </summary>
    public class JobReceptionException : Exception {
        public JobReceptionException() {
        }

        public JobReceptionException(string message) : base(message) {
        }
    }
}