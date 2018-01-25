using System;

namespace MagnumBI.Dispatch.Engine.Exceptions {
    /// <summary>
    ///     Exception to be thrown when there is an error completing a job.
    /// </summary>
    public class CompleteJobException : Exception {
        public CompleteJobException(string message) : base(message) {
        }
    }
}