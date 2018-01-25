using System;

namespace MagnumBI.Dispatch.Engine.Exceptions {
    /// <summary>
    ///     Exception thrown when a job fails to cancel.
    /// </summary>
    public class CancelJobException : Exception {
        public CancelJobException(string message) : base(message) {
        }
    }
}