using Android.App;
using Android.Content.PM;
using Android.OS;
using CipherBank_app.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;

namespace CipherBank_app;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <summary>
    /// Resets the custody idle deadline on any Android user interaction (touch/key).
    /// Use: High (framework callback while activity is resumed). Scope: Android process.
    /// </summary>
    public override void OnUserInteraction()
    {
        base.OnUserInteraction();
        IPlatformApplication.Current?.Services.GetService<AppIdleLockService>()?.Touch();
    }
}
