using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace IoTEdge.Extensions.Hosting
{
    public static class HostBuilderExtensions
    {
        private static bool telemetryHasBeenConfigured = false;

        public static IHostBuilder ConfigureIoTEdgeTelemetry(this IHostBuilder hostBuilder)
        {
            // Allow this method to be called multiple times but only execute the first time it is called.
            if (telemetryHasBeenConfigured)
                return hostBuilder;
            telemetryHasBeenConfigured = true;

            return hostBuilder
                .ConfigureLogging((context, logging) =>
                {
                    // Make logging conform to the IoT Edge recommendations documented here:
                    // https://docs.microsoft.com/en-us/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2018-06#recommended-logging-format
                    logging.ClearProviders();
                    logging.AddSystemdConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = " yyyy-MM-dd hh:mm:ss.fff zzz "; // Wrap with spaces for readability and consistency with IoT Edge logs.
                        options.UseUtcTimestamp = true; // Containers will typically have their clock set to UTC, so just make this consistent.
                    });

                    // Allow using trace logs for local development (and opt-in via environment variables in production).
                    logging.SetMinimumLevel(
                        context.HostingEnvironment.IsDevelopment()
                        ? LogLevel.Trace
                        : LogLevel.Debug);
                })
                .ConfigureServices(services =>
                {
                    // Don't include the default sample metrics in the module telemetry.
                    Metrics.SuppressDefaultMetrics();

                    // Set up a shared module-level Prometheus metrics service.
                    services.AddHostedService<PrometheusService>();

                    // Make a module-aware metrics provider available for use in application code.
                    services.AddSingleton<IModuleMetrics, PrometheusModuleMetrics>();
                });
        }
    }
}
