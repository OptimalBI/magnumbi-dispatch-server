// 
// 0918
// 2017091812:37 PM

using System;
using System.Threading;
using MagnumBI.Dispatch.Web.Models;
using OptimalBI.Collections;
using Serilog;

namespace MagnumBI.Dispatch.Web.Controllers {
    /// <summary>
    ///     Maintains a job status while a client is attempting to handle the job.
    ///     Is responsible for timing out the job if it is not completed.
    /// </summary>
    public static class JobMaintenanceHelper {
        /// <summary>
        ///     The thread for handling job timeouts.
        /// </summary>
        public static Thread JobTimeoutThread;

        private static readonly PriorityLinkedList<JobTimeoutModel> Queue = new PriorityLinkedList<JobTimeoutModel>();
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static readonly AutoResetEvent ResetEvent = new AutoResetEvent(false);

        /// <summary>
        ///     Starts a thread which monitors job status
        /// </summary>
        public static void StartJobTimeoutThread() {
            JobTimeoutThread = new Thread(() => {
                TimeSpan second = TimeSpan.FromSeconds(1);
                while (!TokenSource.IsCancellationRequested) {
                    JobTimeoutModel j;
                    j = GetPriorityJob(); // Get the next due job.
                    if (j == null) {
                        ResetEvent.WaitOne();
                        continue;
                    }

                    int ticks = (int) Math.Ceiling((j.Duetime - DateTime.Now)
                        .TotalSeconds); // Number of ticks until its due.

                    if (ticks > 0) {
                        // Sleep until job is due to be timed out.
                        CheckedSleepHelper.CheckedSleep(ticks, second, TokenSource);
                    }

                    try {
                        if (Program.MagnumBiDispatchController.IsTrackingJob(j.JobId)) {
                            Log.Information($"Job {j.Request.QueueId}:{j.JobId} was not finished, failing.");
                            Program.MagnumBiDispatchController.FailJob(j.JobId);
                        }
                    } catch (Exception e) {
                        Log.Error("JobMaintenanceHelper: Failed to fail job", e, j);
                    }
                }
            }) {
                IsBackground = true,
                Name = "Job Timeout"
            };
            JobTimeoutThread.Start();
        }

        /// <summary>
        ///     Stops the job timeout handling thread.
        /// </summary>
        public static void Stop() {
            TokenSource.Cancel();
        }

        /// <summary>
        ///     Gets a job timeout from the queue.
        /// </summary>
        /// <returns></returns>
        public static JobTimeoutModel GetPriorityJob() {
            lock (Queue) {
                return Queue.Dequeue();
            }
        }

        /// <summary>
        ///     Adds a job to the queue.
        /// </summary>
        /// <param name="j"></param>
        public static void AddJob(JobTimeoutModel j) {
            lock (Queue) {
                Queue.Enqueue(j);
            }
            ResetEvent.Set();
        }

        /// <summary>
        ///     Returns true if this is not handling any jobs.
        /// </summary>
        /// <returns>True if not jobs else false.</returns>
        public static bool NoJobs() {
            lock (Queue) {
                return Queue.Count == 0;
            }
        }

        /// <summary>
        ///     Peeks at the next job due.
        /// </summary>
        /// <returns></returns>
        public static JobTimeoutModel PeekPriorityJob() {
            lock (Queue) {
                return Queue.Peek();
            }
        }

        /// <summary>
        ///     Returns ture if this job exists in the timeout system, else false.
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        public static bool JobExists(JobTimeoutModel j) {
            lock (Queue) {
                return Queue.Contains(j);
            }
        }

        /// <summary>
        ///     Removes a job from the timeout system.
        /// </summary>
        /// <param name="j"></param>
        public static void Remove(JobTimeoutModel j) {
            lock (Queue) {
                Queue.Remove(j);
            }
        }
    }
}