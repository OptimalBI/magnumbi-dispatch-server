using MagnumBI.Dispatch.Engine.Exceptions;

namespace MagnumBI.Dispatch.Engine.Datastore {
    /// <summary>
    ///     Base class for datastores.
    /// </summary>
    public abstract class BaseDatastore : IDatastore {
        public virtual bool HandleJobIds => false;
        public virtual bool SupportStartKeySearch => false;

        /// <inheritdoc />
        public abstract void Add(string qid, Job job);

        /// <inheritdoc />
        public abstract Job Get(string qid, string jobId);

        /// <inheritdoc />
        public abstract void ClearData();

        /// <inheritdoc />
        public abstract bool IsEmpty(string qid);

        /// <inheritdoc />
        public virtual void StoreJobId(string jobId) {
            throw new MethodNotSupportedException();
        }

        /// <inheritdoc />
        public virtual bool IsJobIdAlreadyUsed(string jobId) {
            throw new MethodNotSupportedException();
        }

        /// <inheritdoc />
        public abstract void Close();

        /// <inheritdoc />
        public abstract void ClearQueue(string qid);

        /// <inheritdoc />
        public abstract void Remove(string qid, string jobId);
    }
}