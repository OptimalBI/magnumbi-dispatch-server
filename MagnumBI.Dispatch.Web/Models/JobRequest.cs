// 
// 0918
// 2017091812:37 PM

using System;

namespace MagnumBI.Dispatch.Web.Models {
    /// <summary>
    ///     Request for retrieving a job from a queue.
    /// </summary>
    public class JobRequest {
        /// <summary>
        ///     The number of seconds to wait until failing this job (unless it has been completed).
        /// </summary>
        public int JobHandleTimeoutSeconds = 120;

        /// <summary>
        ///     Name of queue to request job from.
        /// </summary>
        [Obsolete("Use QueueID Instead.")]
        public string AppId {
            get => this.QueueId;
            set => this.QueueId = value;
        }

        /// <summary>
        ///     Name of queue to request job from.
        /// </summary>
        public string QueueId { get; set; }

        /// <summary>
        ///     Maximum number of seconds to wait for a job to appear if there is none.
        /// </summary>
        public int Timeout { get; set; } = -1;

        /// <summary>
        ///     String representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{nameof(this.JobHandleTimeoutSeconds)}: {this.JobHandleTimeoutSeconds}, " +
                   $"{nameof(this.QueueId)}: {this.QueueId}, " +
                   $"{nameof(this.Timeout)}: {this.Timeout}";
        }
    }
}