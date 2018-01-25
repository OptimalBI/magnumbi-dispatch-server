using JetBrains.Annotations;

namespace MagnumBI.Dispatch.Engine.Queue {
    /// <summary>
    ///     Abstraction of a job queue.
    /// </summary>
    public interface IJobQueue {
        /// <summary>
        ///     True iff ready to send and receive jobs.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        ///     Enqueues a new job on the specified queue.
        /// </summary>
        /// <param name="qid">The ID of the queue to queue on.</param>
        /// <param name="jobId">The job ID to enqueue.</param>
        void QueueJob(string qid, string jobId);

        /// <summary>
        ///     Called to start the connection to the queue.
        /// </summary>
        void Connect();

        /// <summary>
        ///     Gets a job off the queue if there is one there, else returns null.
        /// </summary>
        /// <param name="qid">The ID of the queue to retrieve from.</param>
        /// <returns>The ID of the job at the front of the queue if one exists, otherwise null.</returns>
        [CanBeNull]
        string RetrieveJobId(string qid);

        /// <summary>
        ///     Marks a previously retrieved job as completed.
        /// </summary>
        /// <param name="jobId">The ID of the job that has been completed.</param>
        void CompleteJob(string jobId);

        /// <summary>
        ///     Marks a job as failed.
        ///     Called when a client has gone quiet and will not acknowledge job complete requests.
        /// </summary>
        /// <param name="jobId">The ID of the job to fail.</param>
        void FailJob(string jobId);

        /// <summary>
        ///     Returns true iff the specified queue is empty.
        /// </summary>
        /// <param name="qid">The ID of the queue to check.</param>
        /// <returns>True iff the queue with ID qid is empty.</returns>
        bool IsEmpty(string qid);

        /// <summary>
        ///     Resets a queue to its default state.
        /// </summary>
        /// <param name="qid">The ID of the queue to reset.</param>
        void ResetQueue(string qid);

        /// <summary>
        ///     Completely removes a queue from the system.
        /// </summary>
        /// <param name="qid">ID of queue to remove</param>
        void DeleteQueue(string qid);

        /// <summary>
        ///     Creates a new queue. Does not need to be called manually, queues are created automatically if they dont exist.
        /// </summary>
        /// <param name="qid">ID of new queue</param>
        void CreateQueue(string qid);

        /// <summary>
        ///     Tries to check if queue exists. Returns true if the queue definitely exists. If unknown or does not exist returns
        ///     false.
        /// </summary>
        /// <param name="qid">ID of queue to check</param>
        /// <returns>True iff the queue definitely exists.</returns>
        bool QueueExists(string qid);

        /// <summary>
        ///     True iff the job is currently being handled by a client and is not on the queue, but also not completed.
        /// </summary>
        /// <param name="jobId">The ID of the job being tracked.</param>
        /// <returns></returns>
        bool IsTrackingJob(string jobId);

        /// <summary>
        ///     Closes any open connections.
        /// </summary>
        void DisconnectAndClose();
    }
}