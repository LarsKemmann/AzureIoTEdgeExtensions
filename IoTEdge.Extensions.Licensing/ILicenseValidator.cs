using System;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.Extensions.Licensing
{
    public interface ILicenseValidator
    {
        Task ValidateLicenseAsync(string licenseKey,
            string moduleInstanceName, string hostName, string hubName,
            DateTime utcNow, CancellationToken cancellationToken);
    }
}
