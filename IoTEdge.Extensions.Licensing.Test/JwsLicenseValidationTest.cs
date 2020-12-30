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
        static readonly X509SecurityKey issuerCertificateWithPrivateKey;
        static readonly X509SecurityKey issuerCertificatePublicKeyOnly;
        static readonly JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

        // For simplicity, all tests should be defined relative to this timestamp.
        static readonly DateTime now = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        static JwsLicenseValidationTest()
        {
            // The self-signed certificates in this project are valid from 2020-12-30 21:37:41 UTC until 2040-12-30 21:37:41 UTC.
            issuerCertificateWithPrivateKey = new X509SecurityKey(X509Certificate2.CreateFromPemFile(@".\issuer.cer", @".\issuer.key"));
            issuerCertificatePublicKeyOnly = new X509SecurityKey(new X509Certificate2(@".\issuer.cer"));
        }


        [TestMethod]
        public void ValidLicenseIsValidatedWithoutErrors()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub");
        }

        [TestMethod]
        public void ExpiredLicenseErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(-1), issuedAt: now.AddDays(-2), notBefore: now.AddDays(-2),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<SecurityTokenInvalidLifetimeException>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub"));
        }

        [TestMethod]
        public void NotYetValidLicenseErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(2), issuedAt: now.AddDays(-2), notBefore: now.AddDays(1),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<SecurityTokenInvalidLifetimeException>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub"));
        }

        [TestMethod]
        public void IncorrectAudienceErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<SecurityTokenInvalidAudienceException>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "INCORRECT", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub"));
        }

        [TestMethod]
        public void IncorrectIssuerErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<SecurityTokenInvalidIssuerException>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "INCORRECT",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub"));
        }

        [TestMethod]
        public void IncorrectIoTHubNameErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<Exception>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "INCORRECT"));
        }

        [TestMethod]
        public void IncorrectHostNameErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<Exception>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "INCORRECT", hubName: "myiothub"));
        }

        [TestMethod]
        public void IncorrectModuleNameErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<Exception>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "INCORRECT", hostName: "myedgehost", hubName: "myiothub"));
        }

        [TestMethod]
        public void UnspecifiedModuleNameIsValidatedWithoutErrors()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: null, device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                expectedTokenId: "12345", validAudience: "testaudience", validIssuer: "testissuer",
                now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub");
        }

        [TestMethod]
        public void IncorrectTokenIdErrorIsThrownCorrectly()
        {
            var token = CreateJws(id: "12345", audience: "testaudience", issuer: "testissuer",
                expires: now.AddDays(1), issuedAt: now, notBefore: now.AddMinutes(-10),
                module: "modulename", device: "myedgehost", iothub: "myiothub",
                issuerSigningKey: issuerCertificateWithPrivateKey);

            Assert.ThrowsException<Exception>(() =>
                JwsLicenseValidation.Validate(token, issuerCertificatePublicKeyOnly,
                    expectedTokenId: "INCORRECT", validAudience: "testaudience", validIssuer: "testissuer",
                    now, moduleInstanceName: "modulename", hostName: "myedgehost", hubName: "myiothub"));
        }


        private static string CreateJws(string id, string audience, string issuer,
            DateTime expires, DateTime issuedAt, DateTime notBefore,
            string module, string device, string iothub,
            X509SecurityKey issuerSigningKey)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Audience = audience,
                Expires = expires,
                IssuedAt = issuedAt,
                Issuer = issuer,
                NotBefore = notBefore,
                SigningCredentials = new SigningCredentials(issuerSigningKey, SecurityAlgorithms.RsaSha256)
            };

            tokenDescriptor.Claims = new Dictionary<string, object>
            {
                ["jti"] = id,
                ["module"] = module,
                ["device"] = device,
                ["iothub"] = iothub
            };

            return jwtHandler.CreateEncodedJwt(tokenDescriptor);
        }
    }
}
