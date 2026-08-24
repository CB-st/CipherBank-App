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
    public AndroidCertificatePinningHandler()
    {
        // NetworkSecurityConfig.xml (in Resources/xml/network_security_config.xml)
        // automatically enforces certificate pinning for all HTTP requests on Android.
        // No additional configuration needed - Android's network security framework
        // validates certificates against the pinned keys defined in NetworkSecurityConfig.xml.
    }
}
