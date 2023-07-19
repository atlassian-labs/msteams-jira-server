using System;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace MicrosoftTeamsIntegration.Artifacts.Infrastructure.Telemetry
{
    [UsedImplicitly]
    public sealed class CustomTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public CustomTelemetryProcessor(ITelemetryProcessor next) => _next = next;

        public void Process(ITelemetry item)
        {
            if (item.Context.Operation.Name != null)
            {
                if (item.Context.Operation.Name.Equals("GET /", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (item.Context.Operation.Name.Equals("GET /hc", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (item.Context.Operation.Name.Equals("GET /index.html", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (item.Context.Operation.Name.Equals("POST /iisintegration", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            // Filter out synthetic requests
            if (!string.IsNullOrEmpty(item.Context.Operation.SyntheticSource))
            {
                return;
            }

            // Filter out options requests
            if (item is RequestTelemetry request && !string.IsNullOrEmpty(request.Name) &&
                request.Name.StartsWith("options", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _next.Process(item);
        }
    }
}
