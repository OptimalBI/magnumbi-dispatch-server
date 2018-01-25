// 
// 0918
// 2017091812:37 PM

using System;
using System.Collections.Generic;

namespace MagnumBI.Dispatch.Web.Models {
    /// <summary>
    ///     Model used to hold job data for jobs waiting to be timed out.
    /// </summary>
    public class JobTimeoutModel : IComparable<JobTimeoutModel>, IComparable {
        /// <summary>
        ///     The time this job is due to timeout.
        /// </summary>
        public DateTime Duetime { get; set; }

        /// <summary>
        ///     Comparer for comparing due times.
        /// </summary>
        public static Comparer<JobTimeoutModel> DuetimeComparer { get; } = new DuetimeRelationalComparer();

        /// <summary>
        ///     The jobId of the job.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        ///     The model the represents the request.
        /// </summary>
        public JobRequest Request { get; set; }

        /// <inheritdoc />
        public JobTimeoutModel(string jobId, DateTime duetime, JobRequest request) {
            this.JobId = jobId;
            this.Duetime = duetime;
            this.Request = request;
        }

        /// <inheritdoc />
        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) {
                return -1;
            }
            if (ReferenceEquals(this, obj)) {
                return 0;
            }
            if (!(obj is JobTimeoutModel)) {
                throw new ArgumentException($"Object must be of type {nameof(JobTimeoutModel)}");
            }
            return this.CompareTo((JobTimeoutModel) obj);
        }

        /// <inheritdoc />
        public int CompareTo(JobTimeoutModel other) {
            if (ReferenceEquals(this, other)) {
                return 0;
            }
            bool defaultOther = EqualityComparer<JobTimeoutModel>.Default.Equals(other, default(JobTimeoutModel));
            if (defaultOther) {
                return -1;
            }
            if (ReferenceEquals(null, other)) {
                return -1;
            }
            return this.Duetime.CompareTo(other.Duetime);
        }

        /// <inheritdoc />
        public static bool operator <(JobTimeoutModel left, JobTimeoutModel right) {
            return Comparer<JobTimeoutModel>.Default.Compare(left, right) < 0;
        }

        /// <inheritdoc />
        public static bool operator >(JobTimeoutModel left, JobTimeoutModel right) {
            return Comparer<JobTimeoutModel>.Default.Compare(left, right) > 0;
        }

        /// <inheritdoc />
        public static bool operator <=(JobTimeoutModel left, JobTimeoutModel right) {
            return Comparer<JobTimeoutModel>.Default.Compare(left, right) <= 0;
        }

        /// <inheritdoc />
        public static bool operator >=(JobTimeoutModel left, JobTimeoutModel right) {
            return Comparer<JobTimeoutModel>.Default.Compare(left, right) >= 0;
        }

        /// <inheritdoc />
        public override string ToString() {
            return $"Job: {this.JobId} Due: {this.Duetime}";
        }

        private sealed class DuetimeRelationalComparer : Comparer<JobTimeoutModel> {
            public override int Compare(JobTimeoutModel x, JobTimeoutModel y) {
                if (ReferenceEquals(x, y)) {
                    return 0;
                }
                bool defaultX = EqualityComparer<JobTimeoutModel>.Default.Equals(x, default(JobTimeoutModel));
                if (defaultX) {
                    return 1;
                }
                bool defaultY = EqualityComparer<JobTimeoutModel>.Default.Equals(y, default(JobTimeoutModel));
                if (defaultY) {
                    return -1;
                }
                if (ReferenceEquals(null, y)) {
                    return -1;
                }
                if (ReferenceEquals(null, x)) {
                    return 1;
                }
                return x.Duetime.CompareTo(y.Duetime);
            }
        }
    }
}