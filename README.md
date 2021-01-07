# Azure IoT Edge Extensions
These libraries extend [Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge) to:
* integrate with [modern .NET hosting](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1),
* implement IoT Edge recommendations for [logging](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-retrieve-iot-edge-logs?view=iotedge-2018-06) via the standard .NET `ILogger<T>` API,
* provide an easy onboarding path to IoT Edge recommendations for Prometheus-based [metrics](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-access-built-in-metrics?view=iotedge-2018-06),
* implement a robust module licensing client that is compatible with IoT Edge module-oriented licensing servers such as the [WorkSense Edge Licensing Service](https://worksense.io/products/edge-licensing-service).

## Logging & Metrics
Add the `IoTEdge.Extensions.Hosting` NuGet package to your application.

### Logging
In your _Program.cs_ file, add `using IoTEdge.Extensions.Hosting;` and then add a call to `IHostBuilder.ConfigureIoTEdgeTelemetry()` as follows:
```c#
using IoTEdge.Extensions.Hosting;

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
To report metrics from your application, add a reference to the `prometheus-net` NuGet package. In your module service code, add `using IoTEdge.Extensions.Hosting;` and inject an instance of `IModuleMetrics` into your hosted service (or wherever you need to report metrics). The methods on that interface can be used to generate Prometheus metrics instances that are preconfigured with `device_id` and `module_id` labels. From there, you can use the native APIs from [the _prometheus-net_ library](https://github.com/prometheus-net/prometheus-net).
```c#
using IoTEdge.Extensions.Hosting;
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

## Licensing
Add the `IoTEdge.Extensions.Licensing` NuGet package to your application.

In your _Program.cs_ file, add `using IoTEdge.Extensions.Licensing;` and then add a call to `IHostBuilder.ConfigureIoTEdgeOnlineLicensing(...)` as follows, using the URL and public key X.509 signing certificate of the licensing server that is used to sign the licenses:
```c#
using IoTEdge.Extensions.Licensing;
using System.Security.Cryptography.X509Certificates;

public static void Main(string[] args)
{
    // Load the public key certificate (obtained from the licensing server).
    // Remember to include this file in the build output of your project.
    var issuerPublicCert = new X509Certificate2(@".\issuer.cer");

    Host.CreateDefaultBuilder(args)
        // Add this line
        .ConfigureIoTEdgeOnlineLicensing("https://licensing.example.com", issuerPublicCert)
        .ConfigureServices(services =>
        {
            services.AddHostedService<MyModuleService>();
        }).Build().Run();
}
```
This configures the licensing module to enable periodic license checks.

The licensing system can be used to restrict a license key for use with a particular IoT Edge device (using a combination of the device ID and the associated IoT Hub name to guarantee global uniqueness). Keys can optionally be restricted to a single module or to multiple modules on the same device. The code above remains the same in either case.

### Advanced Usage
The licensing validation primitives in this library are designed to allow plugging in other licensing systems. Take a look at the `ILicenseValidator` interface, which can be used in conjunction with the `IHostBuilder.ConfigureIoTEdgeLicensing<T>(...)` method to implement other license validation schemes, including offline licensing. The `JwsLicenseValidation` class provides a public `Validate` method that you can make use of in implementing your own license validation solution.

Similarly, you can extend the `OnlineJwsLicenseValidator` class to provide server-specific defaults and reduce boilerplate code in your module.

## Feedback
This is an alpha release. All feedback, feature requests, bug reports, etc. are welcome! Please use the Issues tab in this repository.