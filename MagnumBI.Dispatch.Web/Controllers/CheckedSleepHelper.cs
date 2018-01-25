// 
// 0918
// 2017091812:37 PM

using System;
using System.Threading;

namespace MagnumBI.Dispatch.Web.Controllers {
    /// <summary>
    ///     Helps with sleeping while not preventing the application from closing.
    /// </summary>
    public static class CheckedSleepHelper {
        /// <summary>
        ///     The minimum number of seconds to sleep
        /// </summary>
        public const int MinCheckedSleepSeconds = 1;

        /// <summary>
        ///     The maximum number of seconds to sleep
        /// </summary>
        public const int MaxCheckedSleepSeconds = 10;

        /// <summary>
        ///     Sleep that checks every now and then for cancellation of the token source.
        ///     Sleeps for sleepTime * ticks.
        /// </summary>
        /// <param name="ticks">Number of ticks to sleep for</param>
        /// <param name="sleepTime">Time to sleep at each tick</param>
        /// <param name="tokenSource">When this source receives cancellation request, stop sleeping</param>
        public static void CheckedSleep(int ticks, TimeSpan sleepTime, CancellationTokenSource tokenSource) {
            // Check args are valid
            if ((sleepTime.TotalSeconds < MinCheckedSleepSeconds) | (sleepTime.TotalSeconds > MaxCheckedSleepSeconds)) {
                throw new ArgumentOutOfRangeException(
                    $"Sleep time must be between{MinCheckedSleepSeconds} and {MaxCheckedSleepSeconds}");
            }

            for (int i = 0; i < ticks; i++) {
                if (tokenSource.IsCancellationRequested) {
                    return;
                }
                Thread.Sleep(sleepTime);
            }
        }

        // TODO alternative method - checkedsleep until time
        // when next job comes in, decide if it is due sooner than job we are sleeping on
        // swap out datetime we are sleeping until to the sooner due job
        // swap out job in JobMainenanceHelper
    }
}