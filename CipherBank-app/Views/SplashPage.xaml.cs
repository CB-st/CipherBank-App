// <copyright file="SplashPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Views;

/// <summary>
/// Cold-start splash (Expo <c>SplashScreen</c>): ink canvas, pulsing diamond mark, session label.
/// Shown while <see cref="AppShell"/> boots custody / local DB.
/// </summary>
public partial class SplashPage : ContentPage
{
    private CancellationTokenSource? _pulseCts;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _pulseCts?.Cancel();
        _pulseCts = new CancellationTokenSource();
        _ = PulseAsync(_pulseCts.Token);
    }

    protected override void OnDisappearing()
    {
        _pulseCts?.Cancel();
        _pulseCts = null;
        base.OnDisappearing();
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

    private async Task PulseAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await PulseRoot.FadeTo(1.0, 800, Easing.SinInOut);
                ct.ThrowIfCancellationRequested();
                await PulseRoot.FadeTo(0.6, 800, Easing.SinInOut);
                ct.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when leaving splash.
        }
    }
}
