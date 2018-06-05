// 
// 0918
// 2017091812:37 PM

using System;
using System.Dynamic;
using System.Threading;
using MagnumBI.Dispatch.Engine;
using MagnumBI.Dispatch.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MagnumBI.Dispatch.Web.Controllers {
    /// <summary>
    ///     Controller to manage job based requests.
    /// </summary>
    [Route("job")]
    public class JobController : Controller {
        private static MagnumBiDispatchController Engine => Program.MagnumBiDispatchController;

        /// <summary>
        ///     Returns the job engine's status.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Index() {
            if (Engine == null) {
                Log.Error("Status request showed engine not present.");
                return this.StatusCode(500);
            }

            if (!Engine.Running) {
                Log.Error("Status request showed engine not connected.");
                return this.StatusCode(500);
            }

            return this.Ok(new {
                Status = "OK"
            });
        }

        /// <summary>
        ///     Mark a job as completed.
        /// </summary>
        /// <param name="completion">Completion task</param>
        /// <returns>Result of completion action.</returns>
        [HttpPost("complete")]
        public IActionResult CompleteJob([FromBody] JobCompletion completion) {
            if (completion == null) {
                return this.BadRequest();
            }

            if (string.IsNullOrWhiteSpace(completion.QueueId)) {
                return this.BadRequest();
            }

            if (string.IsNullOrWhiteSpace(completion.JobId)) {
                return this.BadRequest();
            }

            Log.Information(
                $"Complete job {completion.QueueId}:{completion.JobId} requested by {this.Request.HttpContext.Connection.RemoteIpAddress}",
                completion);
            try {
                Engine.CompleteJob(completion.QueueId, completion.JobId);
            } catch (Exception e) {
                Log.Warning(
                    $"Failed to complete job from {this.Request.HttpContext.Connection.RemoteIpAddress}. Reason {e.Message}");
                return this.BadRequest(new {
                    Error = e.Message
                });
            }

            return this.Ok();
        }

        /// <summary>
        ///     Mark a job as failed.
        /// </summary>
        /// <param name="completion">Completion task</param>
        /// <returns>Result of failure action.</returns>
        [HttpPost("fail")]
        public IActionResult FailJob([FromBody] JobCompletion completion) {
            if (completion == null) {
                return this.BadRequest();
            }

            Log.Information(
                $"Fail job {completion.QueueId}:{completion.JobId} requested by {this.Request.HttpContext.Connection.RemoteIpAddress}",
                completion);
            if (string.IsNullOrWhiteSpace(completion.QueueId)) {
                return this.BadRequest();
            }

            if (string.IsNullOrWhiteSpace(completion.JobId)) {
                return this.BadRequest();
            }

            try {
                Engine.FailJob(completion.JobId);
            } catch (Exception e) {
                return this.BadRequest(new {
                    Error = e.Message
                });
            }

            return this.Ok();
        }

        /// <summary>
        ///     Submits a job to the queue.
        /// </summary>
        /// <param name="submission">Job submission to use</param>
        [HttpPost("submit")]
        public IActionResult SubmitJob([FromBody] JobSubmission submission) {
            if (submission == null) {
                return this.BadRequest(new {
                    Error = $"Submission body invalid"
                });
            }

            Log.Information(
                $"Submit job {submission.QueueId}:{submission.JobId} requested by {this.Request.HttpContext.Connection.RemoteIpAddress}",
                submission);
            if (submission.QueueId == null) {
                return this.BadRequest(new {
                    Error = $"No QueueId"
                });
            }

            if (submission.Data == null) {
                submission.Data = new ExpandoObject();
            }

            Job j = new Job(submission.Data, submission.JobId, null, submission.PreviousJobIds);

            try {
                Engine.QueueJob(submission.QueueId, j);
            } catch (Exception e) {
                Log.Error(e, $"Submit job failed");
                return this.StatusCode(500, new {message = $"Failed to submit job: {e.Message}"});
            }

            return this.Ok();
        }

        /// <summary>
        ///     Request a job from MagnumBI Dispatch.
        ///     Will look for jobs on request.QueueId.
        ///     If there are no jobs then it will wait for request.Timeout seconds
        ///     for a job to appear (it will return as soon as a job is found).
        ///     There is still no job it returns a no jobs message.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A job from queue matching request.QueueId or null if there are no jobs.</returns>
        [HttpPost("request")]
        public IActionResult RequestJob([FromBody] JobRequest request) {
            if (request == null) {
                return this.BadRequest();
            }

            Log.Debug($"Request job requested by {this.Request.HttpContext.Connection.RemoteIpAddress}. {request}");

            string queueId = request.QueueId;

            if (string.IsNullOrWhiteSpace(queueId)) {
                return this.BadRequest($"AppId cannot be null or empty.");
            }

            // Limit timeout
            int timeoutSeconds = request.Timeout;
            if (timeoutSeconds > 20) {
                timeoutSeconds = 20;
            }

            Job job = null;
            try {
                // Quick return path.
                if (Engine.JobWaiting(queueId)) {
                    job = Engine.RetrieveJob(queueId);
                    // Make sure no one else stole our job.
                    if (job != null) {
                        Log.Information(
                            $"Sent job {request.QueueId}:{job.JobId} to {this.Request.HttpContext.Connection.RemoteIpAddress}");
                        this.MaintainJobStatus(job, request);
                        return new JsonResult(job);
                    }
                }

                if (timeoutSeconds <= 0) {
                    Log.Information(
                        $"Job requested by {this.Request.HttpContext.Connection.RemoteIpAddress} on {request.QueueId} but none was found.");
                    return new JsonResult(new {
                        Message = $"No jobs for {queueId}"
                    });
                }

                Job j = this.WaitAndRetrieveJob(queueId, timeoutSeconds);
                if (j == null) {
                    Log.Information(
                        $"Job requested by {this.Request.HttpContext.Connection.RemoteIpAddress} on {request.QueueId} but none was found.");
                    return new JsonResult(new {
                        Message = $"No jobs for {queueId}"
                    });
                }

                Log.Information($"Sent {j.JobId} to {this.Request.HttpContext.Connection.RemoteIpAddress}");
                this.MaintainJobStatus(j, request);
                return new JsonResult(j);
            } catch (Exception e) {
                Log.Error(e, $"Failed to retrieve job.");
                return this.StatusCode(500);
            }
        }

        /// <summary>
        ///     Determines whether the queue is empty.
        /// </summary>
        /// <param name="request">Request for empty queue check</param>
        /// <returns>Empty = true if the queue is empty else false.</returns>
        [HttpPost("isempty")]
        public IActionResult IsQueueEmpty([FromBody] IsEmptyRequest request) {
            if (request == null) {
                return this.BadRequest();
            }

            return new JsonResult(new {
                Empty = !Engine.JobWaiting(request.QueueId)
            });
        }

        /// <summary>
        ///     Clears a queue of all jobs.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Ok if successful.</returns>
        [HttpPost("clear")]
        public IActionResult ClearQueue([FromBody] IsEmptyRequest request) {
            if (request == null) {
                return this.BadRequest();
            }

            try {
                Engine.PurgeJobs(request.QueueId);
            } catch (Exception e) {
                Log.Error(e, $"Failed to clear queue.");
                return this.StatusCode(500);
            }

            return this.Ok();
        }

        /// <summary>
        ///     Adds a job that has been sent to a client to the job maintenance engine.
        /// </summary>
        /// <param name="j"></param>
        /// <param name="request"></param>
        private void MaintainJobStatus(Job j, JobRequest request) {
            DateTime dueDate = DateTime.Now + TimeSpan.FromSeconds(request.JobHandleTimeoutSeconds);
            JobTimeoutModel model = new JobTimeoutModel(j.JobId, dueDate, request);
            JobMaintenanceHelper.AddJob(model);
        }

        /// <summary>
        ///     Waits until a job is able to be retrieved, and then retrieves it if timeout doesn't occur.
        /// </summary>
        /// <param name="appId">Name of queue</param>
        /// <param name="timeoutSeconds">Seconds to pass before timeout occurs</param>
        /// <returns>The retrieved job, or null if timeout occurs.</returns>
        private Job WaitAndRetrieveJob(string appId, int timeoutSeconds) {
            int sleepTimer = 0;
            while (++sleepTimer <= timeoutSeconds) {
                Thread.Sleep(1000);
                if (Engine.JobWaiting(appId)) {
                    Job job = Engine.RetrieveJob(appId);
                    // Make sure no one else stole our job.
                    if (job != null) {
                        return job;
                    }
                }
            }

            return null;
        }
    }
}