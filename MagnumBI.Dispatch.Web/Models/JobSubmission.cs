// 
// 0918
// 2017091812:37 PM

using System;
using System.Dynamic;
using JetBrains.Annotations;
using MagnumBI.Dispatch.Web.Controllers;

namespace MagnumBI.Dispatch.Web.Models {
    /// <summary>
    ///     The model for the SubmitJob method.
    ///     <seealso cref="JobController.SubmitJob" />
    /// </summary>
    public class JobSubmission {
        /// <summary>
        ///     Name of queue we are submitting the job to.
        /// </summary>
        [Obsolete("Use QueueID Insted.")]
        public string AppId {
            get => this.QueueId;
            set => this.QueueId = value;
        }

        /// <summary>
        ///     Job's data.
        /// </summary>
        public ExpandoObject Data { get; set; }

        /// <summary>
        ///     ID of job to submit.
        /// </summary>
        [CanBeNull]
        public string JobId { get; set; }

        /// <summary>
        ///     List of job ids that "caused" this job.
        ///     Used to track job histories through the MagnumBI Depot system.
        /// </summary>
        [CanBeNull]
        public string[] PreviousJobIds { get; set; }

        /// <summary>
        ///     Name of queue we are submitting the job to.
        /// </summary>
        public string QueueId { get; set; }

        /// <summary>
        ///     String representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return
                $"{nameof(this.QueueId)}: {this.QueueId}, " +
                $"{nameof(this.JobId)}: {this.JobId}, " +
                $"{nameof(this.Data)}: {this.Data}, " +
                $"{nameof(this.PreviousJobIds)}: {this.PreviousJobIds}";
        }
    }
}