﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoTEdge.Extensions.Hosting
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureIoTEdgeTelemetry(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureLogging((context, logging) =>
                {
                    // Make logging conform to the IoT Edge recommendations documented here:
                    // https://docs.microsoft.com/en-us/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2018-06#recommended-logging-format
                    logging.ClearProviders();
                    logging.AddSystemdConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.TimestampFormat = " yyyy-MM-dd hh:mm:ss.fff zzz ";
                        options.UseUtcTimestamp = false;
                    });

                    // Allow using trace logs for local development (and opt-in via environment variables in production).
                    logging.SetMinimumLevel(
                        context.HostingEnvironment.IsDevelopment()
                        ? LogLevel.Trace
                        : LogLevel.Debug);
                })
                .ConfigureServices(services =>
                {
                    // Set up a shared module-level Prometheus metrics service.
                    services.AddHostedService<PrometheusService>();

                    // Make a module-aware metrics provider available for use in application code.
                    services.AddSingleton<IModuleMetrics, PrometheusModuleMetrics>();
                });
        }
    }
}
