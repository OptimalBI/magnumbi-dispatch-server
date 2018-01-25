namespace MagnumBI.Dispatch.Engine.Queue {
    /// <summary>
    ///     Base class for job queues.
    /// </summary>
    public abstract class BaseJobQueue : IJobQueue {
        /// <inheritdoc />
        public abstract void QueueJob(string qid, string jobId);

        /// <inheritdoc />
        public abstract string RetrieveJobId(string qid);

        /// <inheritdoc />
        public abstract bool IsEmpty(string qid);

        /// <inheritdoc />
        public abstract void ResetQueue(string qid);

        /// <inheritdoc />
        public abstract void Connect();

        /// <inheritdoc />
        public abstract bool Connected { get; }

        /// <inheritdoc />
        public abstract void DisconnectAndClose();

        /// <inheritdoc />
        public abstract void CompleteJob(string jobId);

        /// <inheritdoc />
        public abstract void FailJob(string jobId);

        /// <inheritdoc />
        public abstract void DeleteQueue(string qid);

        /// <inheritdoc />
        public abstract void CreateQueue(string qid);

        /// <inheritdoc />
        public abstract bool QueueExists(string qid);

        /// <inheritdoc />
        public abstract bool IsTrackingJob(string jobId);

        /// <summary>
        ///     Ensures that this queue is connected and ready to be operated on.
        /// </summary>
        protected void VerifyReadyToGo() {
            if (!this.Connected) {
                this.Connect();
            }
        }
    }
}