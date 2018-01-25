#region FileInfo

// MagnumBI.Dispatch MagnumBI.Dispatch.Web DispatchEnricher.cs
// Created: 20171219
// Edited: 20171219
// By: Timothy Gray (timgray)

#endregion

using System;
using Serilog.Core;
using Serilog.Events;

namespace MagnumBI.Dispatch.Web.Logging {
    /// <summary>
    ///     Log enricher for magnumbi dispatch.
    /// </summary>
    public class DispatchEnricher : ILogEventEnricher {
        /// <summary>
        ///     The property name added to enriched log events.
        /// </summary>
        public const string MachineNamePropertyName = "DispatchMachineName";

        private LogEventProperty cachedProperty;

        /// <inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
            if (!string.IsNullOrWhiteSpace(Environment.MachineName)) {
                this.cachedProperty = this.cachedProperty ??
                                      propertyFactory.CreateProperty(MachineNamePropertyName,
                                          " " + Environment.MachineName);
            } else {
                this.cachedProperty = this.cachedProperty ??
                                      propertyFactory.CreateProperty(MachineNamePropertyName,
                                          "");
            }

            logEvent.AddPropertyIfAbsent(this.cachedProperty);
        }
    }
}