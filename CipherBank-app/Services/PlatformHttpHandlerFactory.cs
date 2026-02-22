// <copyright file="PlatformHttpHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;

namespace CipherBank_app.Services;

/// <summary>
/// Factory for creating platform-specific HTTP handlers with certificate pinning support.
/// </summary>
public static class PlatformHttpHandlerFactory
{
    /// <summary>
    /// Creates an HTTP message handler appropriate for the current platform.
    /// Includes certificate pinning support on iOS, Android, and Windows.
    /// </summary>
    public static HttpMessageHandler CreateHandler()
    {
#if ANDROID
        // Android: Use AndroidClientHandler which respects NetworkSecurityConfig.xml
        return new CipherBank_app.Platforms.Android.AndroidCertificatePinningHandler();
#elif IOS
        // iOS: Use custom NSUrlSessionHandler with certificate pinning
        return new CipherBank_app.Platforms.iOS.IosCertificatePinningHandler();
#elif MACCATALYST
        // Mac Catalyst: Use custom NSUrlSessionHandler with certificate pinning
        return new CipherBank_app.Platforms.MacCatalyst.MacCatalystCertificatePinningHandler();
#elif WINDOWS
        // Windows: Use custom HttpClientHandler with certificate pinning
        return new CipherBank_app.Platforms.Windows.WindowsCertificatePinningHandler();
#else
        // Other platforms: Use default handler (certificate pinning should be implemented)
        return new HttpClientHandler();
#endif
    }
}
