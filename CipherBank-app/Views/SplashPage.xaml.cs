// <copyright file="SplashPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Views;

/// <summary>
/// Cold-start splash (Expo <c>SplashScreen</c>): ink canvas + diamond mark.
/// Shown while <see cref="AppShell"/> boots custody / local DB.
/// </summary>
/// <remarks>
/// Continuous <c>FadeTo</c> pulse was removed: on Android emulator cold start it
/// pegged the UI thread (~100% CPU) before the first frame, so the platform
/// splash never dismissed and Shell.Loaded never ran.
/// </remarks>
public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    /// <summary>Updates the status caption (optional; bootstrap may leave the default).</summary>
    public void SetStatus(string label)
    {
        if (MainThread.IsMainThread)
        {
            StatusLabel.Text = label;
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = label);
        }
    }
}
