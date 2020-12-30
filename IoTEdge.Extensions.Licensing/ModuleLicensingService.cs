using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.Extensions.Licensing
{
    public sealed class ModuleLicensingService : IHostedService
    {
        private readonly ILogger<ModuleLicensingService> logger;
        private readonly ILicenseValidator licenseValidator;
        private readonly string licenseKey;
        private readonly string moduleInstanceName;
        private string hostName;
        private readonly string hubName;
        private readonly TimeSpan licenseCheckInterval;
        private readonly CancellationTokenSource serviceStopping;
        private Task periodicLicenseChecks;

        
        public ModuleLicensingService(ILogger<ModuleLicensingService> logger, ILicenseValidator licenseValidator)
        {
            this.logger = logger;
            this.licenseValidator = licenseValidator;

            moduleInstanceName = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            if (string.IsNullOrWhiteSpace(moduleInstanceName))
                throw new InvalidOperationException($"IOTEDGE_MODULEID environment variable is null or blank.");
            hostName = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            if (string.IsNullOrWhiteSpace(hostName))
                throw new InvalidOperationException($"IOTEDGE_DEVICEID environment variable is null or blank.");
            hubName = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
            if (string.IsNullOrWhiteSpace(hubName))
                throw new InvalidOperationException($"IOTEDGE_IOTHUBHOSTNAME environment variable is null or blank.");
            logger.LogInformation("Beginning periodic license validation for module {Module} on host {Host} for IoT Hub {Hub}",
                moduleInstanceName, hostName, hubName);

            licenseKey = Environment.GetEnvironmentVariable("MODULE_LICENSE_KEY");
            if (string.IsNullOrWhiteSpace(licenseKey))
                throw new InvalidOperationException($"MODULE_LICENSE_KEY environment variable is null or blank.");
            logger.LogInformation("License key '{LicenseKey}' loaded from environment variable", licenseKey);

            licenseCheckInterval = TimeSpan.FromMinutes(15);
            serviceStopping = new CancellationTokenSource();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            periodicLicenseChecks = Task.Run(PeriodicLicenseChecksAsync);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            serviceStopping.Cancel();
            return periodicLicenseChecks;
        }


        private async Task PeriodicLicenseChecksAsync()
        {
            do
            {
                logger.LogTrace("Performing license check");
                try
                {
                    var licenseStatus = await licenseValidator.ValidateLicenseAsync(licenseKey,
                        moduleInstanceName, hostName, hubName, DateTime.UtcNow, serviceStopping.Token);
                    switch (licenseStatus)
                    {
                        case LicenseStatus.Expired:
                            throw new Exception("License is expired");
                        case LicenseStatus.Invalid:
                            throw new Exception("License is invalid");
                        case LicenseStatus.NotYetValid:
                            throw new Exception("License is not yet valid");
                        case LicenseStatus.SuspectedReplay:
                            throw new Exception("Suspected license replay");
                        case LicenseStatus.Valid:
                            logger.LogTrace("License is valid");
                            break;
                    }
                    await Task.Delay(licenseCheckInterval, serviceStopping.Token);
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug("Canceled periodic license checks");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "License validation failed");
                    throw;
                }
            } while (!serviceStopping.IsCancellationRequested);
        }
    }
}
