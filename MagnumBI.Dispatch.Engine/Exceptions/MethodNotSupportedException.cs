using System;

namespace MagnumBI.Dispatch.Engine.Exceptions {
    /// <summary>
    ///     Exception to be thrown when a method is called which is not supported
    ///     by one of the services (eg. database, queue) being used.
    /// </summary>
    public class MethodNotSupportedException : Exception {
        public MethodNotSupportedException() {
        }

        public MethodNotSupportedException(string message) : base(message) {
        }
    }
}