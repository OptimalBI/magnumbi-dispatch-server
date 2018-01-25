// 
// 0918
// 2017091812:37 PM

using System.Dynamic;
using MagnumBI.Dispatch.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace MagnumBI.Dispatch.Web.Controllers.Example {
    /// <summary>
    ///     Hosts the example requests for the job controller.
    /// </summary>
    [Route("job/example")]
    public class JobExampleController : Controller {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        [HttpGet("submit")]
        public IActionResult SubmitJobExample() {
            dynamic eo = new ExpandoObject();
            eo.Example = "hi";
            JobSubmission js = new JobSubmission {
                QueueId = "EXAMPLE",
                Data = eo,
                JobId = "123123",
                PreviousJobIds = new[] {
                    "ASDSAD", "SASAB"
                }
            };

            return this.Json(js);
        }

        /// <summary>
        ///     Returns an example of the data for RequestJob
        ///     <seealso cref="JobController.RequestJob" />
        /// </summary>
        /// <returns></returns>
        [HttpGet("request")]
        public IActionResult RequestJobExample() {
            JobRequest jr = new JobRequest {
                QueueId = "QueueID",
                JobHandleTimeoutSeconds = 120,
                Timeout = 2
            };

            return this.Json(jr);
        }

        /// <summary>
        ///     Returns example for Complete job.
        ///     <seealso cref="JobController.CompleteJob" />
        /// </summary>
        /// <returns></returns>
        [HttpGet("complete")]
        public IActionResult CompleteJobExample() {
            JobCompletion jc = new JobCompletion {
                JobId = "ExampleJobId",
                QueueId = "ExampleAppId"
            };
            return this.Json(jc);
        }

        /// <summary>
        ///     Returns an example for IsEmpty.
        ///     <seealso cref="JobController.IsQueueEmpty" />
        /// </summary>
        /// <returns></returns>
        [HttpGet("isempty")]
        public IActionResult IsQueueEmptyExample() {
            IsEmptyRequest ec = new IsEmptyRequest {
                QueueId = "test"
            };
            return this.Json(ec);
        }
    }
}