# Azure IoT Edge Extensions
This library extends [Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge) to:
* integrate with [modern .NET hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1),
* implement IoT Edge recommendations for [logging](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2018-06) via the standard .NET `ILogger<T>` API,
* provide an easy onboarding path to IoT Edge recommendations for Prometheus-based [metrics](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-access-built-in-metrics?view=iotedge-2018-06).

## Usage
Add the `IoTEdge.Extensions.Hosting` NuGet package to your application.

### Logging
In your _Program.cs_ file, add a call to `IHostBuilder.ConfigureIoTEdgeTelemetry()` as follows:
```c#
public static void Main(string[] args)
{
    Host.CreateDefaultBuilder(args)
        .ConfigureIoTEdgeTelemetry() // Add this line
        .ConfigureServices(services =>
        {
            services.AddHostedService<MyModuleService>();
        }).Build().Run();
}
```
This configures the .NET `ILogger<T>`-based logging infrastructure to use the IoT Edge-recommended logging format. Log messages will be sent to the console (stdout) in a way that allows [the `GetModuleLogs` direct method](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2018-06#retrieve-module-logs) to be used to retrieve logs remotely, filtering by date/time range and severity.

### Metrics
To report metrics from your application, add a reference to the `prometheus-net` NuGet package and then inject an instance of `IModuleMetrics` into your hosted service (or wherever you need to report metrics). The methods on that interface can be used to generate Prometheus metrics instances that are preconfigured with `device_id` and `module_id` labels. From there, you can use the native APIs from [the _prometheus-net_ library](https://github.com/prometheus-net/prometheus-net).
```c#
using Prometheus;

public sealed class MyModuleService : IHostedService
{
    private readonly ILogger<MyModuleService> logger;
    private readonly IHistogram processingDuration;

    public MyModuleService(
        IModuleMetrics metrics, // Using constructor injection
        ILogger<MyModuleService> logger)
    {
        this.logger = logger;

        requestDuration = metrics.CreateDurationHistogram(
            "mymodule_processing_duration_seconds",
            "Processing duration (in seconds)");
    }

    // ... other logic

    private Task ProcessAsync()
    {
        using (processingDuration.NewTimer())
        {
            // Do work here
        }
    }
}
```

### Feedback
This is an alpha release. All feedback, bug reports, etc. are welcome! Please use the Issues tab in this repository.