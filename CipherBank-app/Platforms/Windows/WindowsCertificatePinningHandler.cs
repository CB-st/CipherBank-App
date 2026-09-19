// <copyright file="WindowsCertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CipherBank_app.Security;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Platforms.Windows;

/// <summary>Windows HTTP handler enforcing the shared SPKI pin policy.</summary>
public sealed class WindowsCertificatePinningHandler : HttpClientHandler
{
    private readonly ILogger<WindowsCertificatePinningHandler> _logger;

    public WindowsCertificatePinningHandler(
        ILogger<WindowsCertificatePinningHandler> logger)
    {
        _logger = logger;
        ServerCertificateCustomValidationCallback = ValidateServerCertificate;
    }

    private bool ValidateServerCertificate(
        HttpRequestMessage request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        string hostname = request.RequestUri?.Host ?? string.Empty;
        if (!CertificatePinPolicy.RequiresPinning(hostname))
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        if (sslPolicyErrors != SslPolicyErrors.None || certificate is null)
        {
            _logger.LogWarning(
                "Certificate validation failed for pinned host {Hostname}: {Errors}",
                hostname,
                sslPolicyErrors);
            return false;
        }

        return ValidateCertificatePin(certificate, hostname);
    }

    private bool ValidateCertificatePin(X509Certificate2 certificate, string hostname)
    {
        try
        {
            byte[]? spki = certificate.GetRSAPublicKey()?.ExportSubjectPublicKeyInfo()
                ?? certificate.GetECDsaPublicKey()?.ExportSubjectPublicKeyInfo();
            if (spki is null)
            {
                _logger.LogWarning("Unsupported certificate key type for {Hostname}", hostname);
                return false;
            }

            string pin = CertificatePinPolicy.ComputeSpkiSha256Pin(spki);
            bool matched = CertificatePinPolicy.Matches(hostname, pin);
            _logger.LogDebug(
                "Certificate pin validation for {Hostname}: {Matched}",
                hostname,
                matched);
            return matched;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Certificate pin validation failed for {Hostname}", hostname);
            return false;
        }
    }
}
