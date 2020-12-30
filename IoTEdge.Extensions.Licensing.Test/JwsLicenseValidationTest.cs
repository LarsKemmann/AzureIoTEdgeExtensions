using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace IoTEdge.Extensions.Licensing.Test
{
    [TestClass]
    public class JwsLicenseValidationTest
    {
        [TestMethod]
        public void ValidLicenseStatusIsReturnedCorrectly()
        {
            var jwtHandler = new JwtSecurityTokenHandler();

            var issuerPrivateKey = new X509SecurityKey(new X509Certificate2(Base64UrlEncoder.DecodeBytes(
                @"MIICdgIBADANBgkqhkiG9w0BAQEFAASCAmAwggJcAgEAAoGBAMcbxmGS/pQ6ulE1xlgPydiJxLfMBLhpXyT7aQv/zuZLNFnjqo+dtogIRckEdHdnPrLdKwVqIcg/3HCwSIeQrOFWpHpgrSTAjy0y0atKLA2LP79bJsjSTGh/BGHAWajYQsaRYNggnPW5O8p4rmPNGUXEeR6Lm0xlgHoTqng2vEDRAgMBAAECgYBk6GCsBtFa0kCm87fn3WiQg5HdDFrAEzcTCQ9981EqSabof4drbaSaYIbtj4JvGTYfdBNflSA12pefzNeVTO8wB9h+oG7wj3YCcoQSra34k79PY53uGks3G5aqcPPpMjC6ojIrNmHeSFKECKwSRqdmKKvgFzn6CXDR6TLTaNohmQJBAOVNASQaUg6D+icuvGTzSEdz4yU4KKmzWoHelthPICst1wgH3RKLX9cVgBonNiraon77Zk1ksSayyF4rhGwZO5MCQQDeSsx0v/N3D5P421QBaT/VSSVlE7d6468l/+bfXBQ+kiWbGYVbExtBDgJDZYESEaZmctC2vn85mhrDaP8T9HiLAkAB+vZFj4yh33XrnLW30XoQU+nkSmXfgVMIyBlZaOWIOe8ffKHmJRoAy4i9sRUArb61hgpOJM563RRp68pK/LTNAkEA2vOB/10ySgumDHC8hcdNgJ/TnYOWLg0l75/noAnqRtddAzBYEiT3q1RJFmlcgJex9ycQPW/VkL8hrWg2F0mtkQJAD8KXA/qXgdn40mKGuMWtqbdGQvqQ9j9ODfwlqXqV7GzkBt4XFm8fcZtluKeyzGzuLVD3ZmMdaApsu+sk6iJ5Jg==")));

            var issuerPublicKey = new X509SecurityKey(new X509Certificate2(Base64UrlEncoder.DecodeBytes(
                @"MIICVDCCAb2gAwIBAgIBADANBgkqhkiG9w0BAQ0FADBHMQswCQYDVQQGEwJ1czELMAkGA1UECAwCTkMxEjAQBgNVBAoMCUFjbWUgSW5jLjEXMBUGA1UEAwwObGljZW5zaW5nLnRlc3QwHhcNMjAxMjI5MjM0MzE0WhcNMjExMjI5MjM0MzE0WjBHMQswCQYDVQQGEwJ1czELMAkGA1UECAwCTkMxEjAQBgNVBAoMCUFjbWUgSW5jLjEXMBUGA1UEAwwObGljZW5zaW5nLnRlc3QwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBAMcbxmGS/pQ6ulE1xlgPydiJxLfMBLhpXyT7aQv/zuZLNFnjqo+dtogIRckEdHdnPrLdKwVqIcg/3HCwSIeQrOFWpHpgrSTAjy0y0atKLA2LP79bJsjSTGh/BGHAWajYQsaRYNggnPW5O8p4rmPNGUXEeR6Lm0xlgHoTqng2vEDRAgMBAAGjUDBOMB0GA1UdDgQWBBQQ10LXCor/5F6OcrbHXjd2LvRQIDAfBgNVHSMEGDAWgBQQ10LXCor/5F6OcrbHXjd2LvRQIDAMBgNVHRMEBTADAQH/MA0GCSqGSIb3DQEBDQUAA4GBAHyp95Wh2iHXEQXQQ1FcnxqKt9/VmU1t+zKFJnZWTUuD9XxDvWsM5fPHMTdVYu6qpYi46aTO1iWdhW2C84hhU/eJynkmXvRwxBaeIT1d9fr1sG8U1SYVKxLnywCDnRkijezLOtZRsS1nj+NDJXTQu4zJnpuDflTQufVgflWpKrfw")));

            var now = new DateTime(2020, 12, 30, 0, 0, 0, DateTimeKind.Utc);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = "AUDIENCE",
                Expires = now.AddDays(1),
                IssuedAt = now,
                Issuer = "ISSUER",
                NotBefore = now.AddMinutes(-10),
                SigningCredentials = new SigningCredentials(issuerPrivateKey, SecurityAlgorithms.HmacSha256)
            };
            tokenDescriptor.Claims = new Dictionary<string, object>
            {
                ["jti"] = "12345",
                ["module"] = "modulename",
                ["device"] = "myedgehost",
                ["iothub"] = "myiothub"
            };

            var token = jwtHandler.CreateEncodedJwt(tokenDescriptor);

            JwsLicenseValidation.Validate(token, issuerPublicKey,
                expectedTokenId: "12345", validAudience: "AUDIENCE", validIssuer: "ISSUER",
                now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub");
        }
    }
}
