// <copyright file="AndroidCertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Platforms.Android;

/// <summary>
/// Android-specific HTTP handler.
/// NetworkSecurityConfig.xml automatically handles certificate pinning for all HTTP requests.
/// This handler uses the default HttpClientHandler which respects NetworkSecurityConfig.xml.
/// </summary>
public class AndroidCertificatePinningHandler : HttpClientHandler
{
}
