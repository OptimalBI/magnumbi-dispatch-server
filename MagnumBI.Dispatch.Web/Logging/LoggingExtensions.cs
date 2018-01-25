#region FileInfo

// MagnumBI.Dispatch MagnumBI.Dispatch.Web LoggingExtensions.cs
// Created: 20171219
// Edited: 20171219
// By: Timothy Gray (timgray)

#endregion

using System;
using Serilog;
using Serilog.Configuration;

namespace MagnumBI.Dispatch.Web.Logging {
    /// <summary>
    ///     Extensions for serilog.
    /// </summary>
    public static class LoggingExtensions {
        public static LoggerConfiguration WithDispatchMachineName(
            this LoggerEnrichmentConfiguration enrichmentConfiguration) {
            if (enrichmentConfiguration == null) {
                throw new NullReferenceException();
            }
            return enrichmentConfiguration.With<DispatchEnricher>();
        }
    }
}