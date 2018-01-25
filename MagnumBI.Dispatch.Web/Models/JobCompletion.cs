// 
// 0918
// 2017091812:37 PM

using System;

namespace MagnumBI.Dispatch.Web.Models {
    /// <summary>
    ///     The model for the JobCompletion method.
    /// </summary>
    public class JobCompletion {
        /// <summary>
        ///     Name of queue to request complete job on.
        /// </summary>
        [Obsolete("Use QueueID Insted.")]
        public string AppId {
            get => this.QueueId;
            set => this.QueueId = value;
        }

        /// <summary>
        ///     ID of job to complete.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        ///     Name of queue to request complete job on.
        /// </summary>
        public string QueueId { get; set; }

        /// <summary>
        ///     Returns the string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{nameof(this.QueueId)}: {this.QueueId}, {nameof(this.JobId)}: {this.JobId}";
        }
    }
}