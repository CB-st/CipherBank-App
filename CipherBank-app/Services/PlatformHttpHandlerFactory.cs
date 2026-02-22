using System.Net.Http;
using Microsoft.Maui.ApplicationModel;

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
        return new Platforms.Android.AndroidCertificatePinningHandler();
#elif IOS || MACCATALYST
        // iOS/Mac Catalyst: Use custom NSUrlSessionHandler with certificate pinning
        return new Platforms.iOS.IosCertificatePinningHandler();
#elif WINDOWS
        // Windows: Use custom HttpClientHandler with certificate pinning
        return new Platforms.Windows.WindowsCertificatePinningHandler();
#else
        // Other platforms: Use default handler (certificate pinning should be implemented)
        return new HttpClientHandler();
#endif
    }
}
