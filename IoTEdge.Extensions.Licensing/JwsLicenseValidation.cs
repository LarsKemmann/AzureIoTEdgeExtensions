using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace IoTEdge.Extensions.Licensing
{
    public static class JwsLicenseValidation
    {
        private static readonly JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();


        public static void Validate(string jws, X509SecurityKey issuerPublicKey,
            string expectedTokenId, string validAudience, string validIssuer, DateTime utcNow,
            string moduleInstanceName, string hostName, string hubName)
        {
            var validationParameters = new TokenValidationParameters
            {
                AuthenticationType = nameof(JwsLicenseValidation),
                IssuerSigningKey = issuerPublicKey,
                LifetimeValidator =
                    // Use a custom lifetime validator to allow for unit testing with mock system clock values.
                    (DateTime? notBefore, DateTime? expires, SecurityToken _, TokenValidationParameters __) =>
                    {
                        if (notBefore == null || expires == null)
                        {
                            return false;
                        }
                        if (utcNow < notBefore)
                        {
                            return false;
                        }
                        else if (utcNow > expires)
                        {
                            return false;
                        }
                        else
                            return true;
                    },
                ValidAudience = validAudience,
                ValidIssuer = validIssuer,
            };

            var claimsPrincipal = jwtHandler.ValidateToken(jws, validationParameters, out var validatedToken);

            if (expectedTokenId != null && validatedToken.Id != expectedTokenId)
                throw new Exception("The token ID in the license did not match the expected value");

            if (claimsPrincipal.FindFirst("device").Value != moduleInstanceName)
                throw new Exception("The IoT Edge device name in the license did not match the expected value");

            if (claimsPrincipal.FindFirst("iothub").Value != moduleInstanceName)
                throw new Exception("The IoT Hub name in the license did not match the expected value");

            // The module constraint is optional -- enforce it only if it was specified in the JWS.
            var moduleClaim = claimsPrincipal.FindFirst("module")?.Value;
            if (moduleClaim != null && moduleClaim != moduleInstanceName)
                throw new Exception("The module instance name was specified in the license and did not match the expected value");
        }
    }
}
