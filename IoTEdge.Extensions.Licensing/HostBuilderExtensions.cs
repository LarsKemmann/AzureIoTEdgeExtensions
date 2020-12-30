using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;

namespace IoTEdge.Extensions.Licensing
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureIoTEdgeOnlineLicensing(this IHostBuilder hostBuilder,
            string licensingServerUrl, X509SecurityKey issuerPublicKey) =>
            ConfigureIoTEdgeLicensing(hostBuilder, _ =>
                new OnlineJwsLicenseValidator(licensingServerUrl, issuerPublicKey));

        public static IHostBuilder ConfigureIoTEdgeLicensing<T>(this IHostBuilder hostBuilder,
            Func<IServiceProvider, T> licenseValidatorFactory)
            where T : ILicenseValidator
        {
            return hostBuilder
                .ConfigureServices(services =>
                {
                    // Set up a shared module-level IoT Edge module licensing service.
                    services.AddHostedService(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<ModuleLicensingService>>();
                        var licenseProvider = licenseValidatorFactory(serviceProvider);
                        return new ModuleLicensingService(logger, licenseProvider);
                    });
                });
        }
    }
}
