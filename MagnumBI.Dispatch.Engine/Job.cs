using System;
using System.Dynamic;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace MagnumBI.Dispatch.Engine {
    /// <summary>
    ///     A job to be stored on the queue.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Job {
        private bool completed;

        /// <summary>
        ///     The data associated with this object.
        /// </summary>
        [JsonProperty]
        public dynamic Data;

        /// <summary>
        ///     The private part of the job id.
        /// </summary>
        [CanBeNull]
        private string jobId;

        /// <summary>
        ///     True if this job has been finished
        /// </summary>
        public bool Completed {
            get => this.completed;
            set {
                if (value) {
                    this.FinishDateTime = DateTime.UtcNow.ToString("R");
                }
                this.completed = value;
            }
        }

        /// <summary>
        ///     DateTime when this job was finished, null if never finished.
        /// </summary>
        [CanBeNull]
        public string FinishDateTime { get; set; }

        /// <summary>
        ///     The id of this object.
        /// </summary>
        [NotNull]
        [JsonProperty]
        public string JobId {
            get {
                if (this.jobId != null) {
                    return this.jobId;
                }
                this.jobId = new Guid().ToString("N").ToUpper();
                return this.jobId ?? "";
            }
            private set => this.jobId = value;
        }

        /// <summary>
        ///     The job that triggered this job.
        /// </summary>
        [CanBeNull]
        public string[] PreviousJobIds { get; protected set; }

        /// <summary>
        ///     The start time and date of this job.
        /// </summary>
        public string StartDateTime { get; set; }

        /// <summary>
        ///     Creates a new job with the specified job id.
        /// </summary>
        /// <param name="data">The data for this job to process</param>
        /// <param name="jobId">Id of this job</param>
        /// <param name="startDateTime">The time this job first appeared</param>
        /// <param name="previousJobIds">The job chain that triggered this job</param>
        public Job(dynamic data,
            string jobId = null,
            DateTime? startDateTime = null,
            params string[] previousJobIds) {
            this.JobId = !string.IsNullOrWhiteSpace(jobId) ? jobId : MagnumBiDispatchController.NewJobId();
            this.PreviousJobIds = previousJobIds;
            this.StartDateTime = (startDateTime ?? DateTime.UtcNow).ToString("R");
            this.Data = data ?? new ExpandoObject();
        }

        /// <summary>
        ///     Determines if a Job is equal to this Job.
        /// </summary>
        /// <param name="other">Job to compare this to</param>
        /// <returns>True iff this Job and the given Job are equal</returns>
        protected bool Equals(Job other) {
//            if (this.Completed != other.Completed) {
//                return false;
//            }
//            if (this.jobId != other.jobId) {
//                return false;
//            }
//            if (this.Data != other.Data) {
//                return false;
//            }
//            if (this.StartDateTime != other.StartDateTime) {
//                return false;
//            }
//            if (this.FinishDateTime != other.FinishDateTime) {
//                return false;
//            }
//            return true;
            return this.jobId == other.jobId;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            Job other = obj as Job;
            return other != null && this.Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = this.completed.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Data != null ? this.Data.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.jobId != null ? this.jobId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.PreviousJobIds != null ? this.PreviousJobIds.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.FinishDateTime.GetHashCode();
                hashCode = (hashCode * 397) ^ this.StartDateTime.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Converts this object to its JSON equivalent.
        /// </summary>
        /// <returns>A JSON equivalent of this Job</returns>
        internal string ToJson() {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        ///     Creates a Job from its JSON equivalent.
        /// </summary>
        /// <param name="json">The JSON to create a Job from</param>
        /// <returns>The Job equivalent of the given JSON</returns>
        internal static Job FromJson(string json) {
            return JsonConvert.DeserializeObject<Job>(json);
        }
    }
}