namespace MagnumBI.Dispatch.Engine.Datastore {
    /// <summary>
    ///     Abstraction of a datastore.
    /// </summary>
    public interface IDatastore {
        /// <summary>
        ///     Specifies if this database can help with checking unique job IDs.
        /// </summary>
        bool HandleJobIds { get; }

        /// <summary>
        ///     True iff start keys can be searched.
        /// </summary>
        bool SupportStartKeySearch { get; }

        /// <summary>
        ///     Add row to datastore.
        /// </summary>
        /// <param name="qid">ID of queue job is on.</param>
        /// <param name="job">The job to store data for.</param>
        /// <returns>The primary key of the item added, or an empty string if adding failed.</returns>
        void Add(string qid, Job job);

        /// <summary>
        ///     Get a row from the datastore with the given Job.
        /// </summary>
        /// <param name="qid">ID of queue job is on.</param>
        /// <param name="jobId">The job ID of the data you want.</param>
        /// <returns>The contents of the datastore row: [qid, part, jobId, data]</returns>
        Job Get(string qid, string jobId);

        /// <summary>
        ///     Removes a job reference from the datastore.
        /// </summary>
        /// <param name="qid">ID of queue job is on.</param>
        /// <param name="jobId">The job ID.</param>
        void Remove(string qid, string jobId);

        /// <summary>
        ///     Empties the database.
        /// </summary>
        void ClearData();

        /// <summary>
        ///     Empties the table.
        /// </summary>
        /// <param name="qid">ID of queue</param>
        void ClearQueue(string qid);

        /// <summary>
        ///     Determines whether the table is empty.
        /// </summary>
        /// <returns>True iff the table is empty.</returns>
        bool IsEmpty(string qid);

        /// <summary>
        ///     Stores a newly used job ID.
        /// </summary>
        /// <param name="jobId">Job ID to store</param>
        void StoreJobId(string jobId);

        /// <summary>
        ///     Checks if a job id has been used before.
        /// </summary>
        /// <param name="jobId">Job ID to check</param>
        /// <returns></returns>
        bool IsJobIdAlreadyUsed(string jobId);

        /// <summary>
        ///     Dispose of datastore resources.
        /// </summary>
        void Close();
    }
}