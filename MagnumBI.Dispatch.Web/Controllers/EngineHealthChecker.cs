// 
// 0918
// 2017091812:37 PM

using System;
using System.Threading;
using Serilog;

namespace MagnumBI.Dispatch.Web.Controllers {
    /// <summary>
    ///     Checks the health of the MagnumBI Dispatch engine while it is running.
    /// </summary>
    public static class EngineHealthChecker {
        /// <summary>
        ///     The thread used to monitor health.
        /// </summary>
        public static Thread MonitorThread;

        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        /// <summary>
        ///     Starts engine health checck thread.
        /// </summary>
        public static void StartMonitorThread() {
            MonitorThread = new Thread(() => {
                int sleepCount = 0;
                while (!TokenSource.IsCancellationRequested) {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    // Check health every minute
                    if (++sleepCount > 60) {
                        sleepCount = 0;
                        // Check MMM controller
                        if (Program.MagnumBiDispatchController == null ||
                            !Program.MagnumBiDispatchController.Running) {
                            Log.Warning("Engine health check failed, reconnecting.");
                            try {
                                Program.MagnumBiDispatchController?.Shutdown();
                                Program.CreateAndConnectEngine();
                            } catch (Exception e) {
                                Log.Error("Engine health check failed to reconnect!", e);
                            }
                        }
                        // Check job timeout thread
                        if (JobMaintenanceHelper.JobTimeoutThread == null ||
                            !JobMaintenanceHelper.JobTimeoutThread.IsAlive) {
                            Log.Warning("Job Timeout Thread not running. Restarting thread.");
                            JobMaintenanceHelper.StartJobTimeoutThread();
                        }
                    }
                }
            }) {
                IsBackground = true,
                Name = "Monitor"
            };
            MonitorThread.Start();
        }

        /// <summary>
        ///     Stops the EngineHealthChecker.
        /// </summary>
        public static void Stop() {
            TokenSource.Cancel();
        }
    }
}