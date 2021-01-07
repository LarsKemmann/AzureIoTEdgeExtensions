using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
        private readonly AsyncRetryPolicy exponentialBackoffPolicy;


        public OnlineJwsLicenseValidator(string licensingServerUrl, X509Certificate2 issuerPublicKey)
        {
            this.licensingServerUrl = licensingServerUrl;
            this.issuerPublicKey = new X509SecurityKey(issuerPublicKey);

            validAudience = Assembly.GetEntryAssembly().FullName;

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(licensingServerUrl),
                Timeout = TimeSpan.FromSeconds(60)
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/jwt"));

            exponentialBackoffPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(8),
                    TimeSpan.FromSeconds(30)
                });
        }


        public async Task ValidateLicenseAsync(string licenseKey,
            string moduleInstanceName, string hostName, string hubName,
            DateTime utcNow, CancellationToken cancellationToken)
        {
            var requestNonce = Guid.NewGuid().ToString("D");

            var formValues = new Dictionary<string, string>
            {
                ["nonce"] = requestNonce,
                ["key"] = licenseKey
            };

            var licenseJwsResult = await exponentialBackoffPolicy.ExecuteAndCaptureAsync(async (innerCancellationToken) =>
            {
                var response = await httpClient.PostAsync("", new FormUrlEncodedContent(formValues), innerCancellationToken);
                response.EnsureSuccessStatusCode();
                var jws = await response.Content.ReadAsStringAsync();
                return jws;
            }, cancellationToken);

            if (licenseJwsResult.Outcome != OutcomeType.Successful)
                throw licenseJwsResult.FinalException;

            JwsLicenseValidation.Validate(licenseJwsResult.Result, issuerPublicKey,
                expectedTokenId: requestNonce, validAudience, validIssuer: licensingServerUrl, utcNow,
                moduleInstanceName, hostName, hubName);
        }
    }
}
