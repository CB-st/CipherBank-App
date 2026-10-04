// <copyright file="StartupPage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Views;

/// <summary>Blocks the product shell until required local startup work succeeds.</summary>
public sealed class StartupPage : ContentPage
{
    private readonly ActivityIndicator _indicator;
    private readonly Label _message;
    private readonly Button _retry;

    public StartupPage()
    {
        _indicator = new ActivityIndicator { IsRunning = true, IsVisible = true };
        _message = new Label
        {
            Text = "Preparing CipherBank…",
            HorizontalTextAlignment = TextAlignment.Center,
        };
        _retry = new Button { Text = "Retry", IsVisible = false };
        Content = new VerticalStackLayout
        {
            Padding = 32,
            Spacing = 16,
            VerticalOptions = LayoutOptions.Center,
            Children = { _indicator, _message, _retry },
        };
    }

    public void SetRetry(Func<Task> retry)
    {
        ArgumentNullException.ThrowIfNull(retry);
        _indicator.IsRunning = false;
        _indicator.IsVisible = false;
        _message.Text = "CipherBank could not initialize local data.";
        _retry.IsVisible = true;
        _retry.Clicked += async (_, _) =>
        {
            _retry.IsVisible = false;
            _indicator.IsVisible = true;
            _indicator.IsRunning = true;
            _message.Text = "Retrying…";
            await retry();
        };
    }
}
