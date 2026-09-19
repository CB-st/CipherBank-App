// <copyright file="WindowsCertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CipherBank_app.Security;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Platforms.Windows;

/// <summary>Windows HTTP handler enforcing the shared SPKI pin policy.</summary>
public sealed partial class WindowsCertificatePinningHandler : HttpClientHandler
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
            LogCertificateValidationFailed(_logger, hostname, sslPolicyErrors);
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
                LogUnsupportedCertificateKeyType(_logger, hostname);
                return false;
            }

            string pin = CertificatePinPolicy.ComputeSpkiSha256Pin(spki);
            bool matched = CertificatePinPolicy.Matches(hostname, pin);
            LogCertificatePinValidation(_logger, hostname, matched);
            return matched;
        }
        catch (Exception ex)
        {
            LogCertificatePinValidationFailed(_logger, ex, hostname);
            return false;
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Warning, Message = "Certificate validation failed for pinned host {Hostname}: {Errors}")]
    private static partial void LogCertificateValidationFailed(ILogger logger, string hostname, SslPolicyErrors errors);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported certificate key type for {Hostname}")]
    private static partial void LogUnsupportedCertificateKeyType(ILogger logger, string hostname);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Certificate pin validation for {Hostname}: {Matched}")]
    private static partial void LogCertificatePinValidation(ILogger logger, string hostname, bool matched);

    [LoggerMessage(Level = LogLevel.Error, Message = "Certificate pin validation failed for {Hostname}")]
    private static partial void LogCertificatePinValidationFailed(ILogger logger, Exception ex, string hostname);
#pragma warning restore SA1204 // Static members should appear before non-static members
}
