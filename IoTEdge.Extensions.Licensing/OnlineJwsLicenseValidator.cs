using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.Extensions.Licensing
{
    public class OnlineJwsLicenseValidator : ILicenseValidator
    {
        private const string AUTHENTICATION_TYPE = nameof(OnlineJwsLicenseValidator);

        private readonly string licensingServerUrl;
        private readonly X509SecurityKey issuerPublicKey;
        private readonly string validAudience;
        private readonly HttpClient httpClient;


        public OnlineJwsLicenseValidator(string licensingServerUrl, X509SecurityKey issuerPublicKey)
        {
            this.licensingServerUrl = licensingServerUrl;
            this.issuerPublicKey = issuerPublicKey;

            validAudience = Assembly.GetEntryAssembly().FullName;

            httpClient = new HttpClient { BaseAddress = new Uri(licensingServerUrl), Timeout = TimeSpan.FromSeconds(60) };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/jwt"));
        }


        public async Task<LicenseStatus> ValidateLicenseAsync(string licenseKey,
            string moduleInstanceName, string hostName, string hubName,
            DateTime utcNow, CancellationToken cancellationToken)
        {
            var requestNonce = Guid.NewGuid().ToString("D");

            var formValues = new Dictionary<string, string>
            {
                ["nonce"] = requestNonce,
                ["key"] = licenseKey
            };

            var licenseResponse = await httpClient.PostAsync("", new FormUrlEncodedContent(formValues), cancellationToken);
            licenseResponse.EnsureSuccessStatusCode();
            var licenseJws = await licenseResponse.Content.ReadAsStringAsync();

            return JwsLicenseValidation.Validate(licenseJws, issuerPublicKey,
                expectedTokenId: requestNonce, validAudience, validIssuer: licensingServerUrl, utcNow,
                moduleInstanceName, hostName, hubName);
        }
    }
}
