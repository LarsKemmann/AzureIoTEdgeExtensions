using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace IoTEdge.Extensions.Licensing
{
    public static class JwsLicenseValidation
    {
        private static readonly JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();


        public static LicenseStatus Validate(string jws, X509SecurityKey issuerPublicKey,
            string expectedTokenId, string validAudience, string validIssuer, DateTime utcNow,
            string moduleInstanceName, string hostName, string hubName)
        {
            var status = LicenseStatus.Invalid;

            var validationParameters = new TokenValidationParameters
            {
                AuthenticationType = nameof(JwsLicenseValidation),
                IssuerSigningKey = issuerPublicKey,
                LifetimeValidator =
                    (DateTime? notBefore, DateTime? expires, SecurityToken _, TokenValidationParameters __) =>
                    {
                        if (notBefore == null || expires == null)
                        {
                            return false;
                        }
                        if (utcNow < notBefore)
                        {
                            status = LicenseStatus.NotYetValid;
                            return false;
                        }
                        else if (utcNow > expires)
                        {
                            status = LicenseStatus.Expired;
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
            {
                status = LicenseStatus.SuspectedReplay;
            }
            else
            {
                var licensedModule = claimsPrincipal.FindFirst("module").Value;
                var licensedDevice = claimsPrincipal.FindFirst("device").Value;
                var licensedIoTHub = claimsPrincipal.FindFirst("iothub").Value;

                if (licensedModule == moduleInstanceName &&
                    licensedDevice == hostName &&
                    licensedIoTHub == hubName)
                    status = LicenseStatus.Valid;
            }

            return status;
        }
    }
}
