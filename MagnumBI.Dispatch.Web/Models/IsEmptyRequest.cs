// 
// 0918
// 2017091812:37 PM

using System;

namespace MagnumBI.Dispatch.Web.Models {
    /// <summary>
    ///     Request for checking if a queue is empty.
    /// </summary>
    public class IsEmptyRequest {
        /// <summary>
        ///     Name of queue to request job from.
        /// </summary>
        [Obsolete("Use QueueID Insted.")]
        public string AppId {
            get => this.QueueId;
            set => this.QueueId = value;
        }

        /// <summary>
        ///     Name of queue to request job from.
        /// </summary>
        public string QueueId { get; set; }
    }
}