// <copyright file="ConnectionChip.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>Expo-style live/offline pill for the Home header.</summary>
public sealed class ConnectionChip : ContentView
{
    public static readonly BindableProperty IsOnlineProperty = BindableProperty.Create(
        nameof(IsOnline),
        typeof(bool),
        typeof(ConnectionChip),
        false,
        propertyChanged: (b, _, _) => ((ConnectionChip)b).Apply());

    private readonly BoxView _dot;
    private readonly Label _label;
    private readonly Border _frame;

    public ConnectionChip()
    {
        _dot = new BoxView
        {
            WidthRequest = 6,
            HeightRequest = 6,
            CornerRadius = 3,
            VerticalOptions = LayoutOptions.Center,
        };

        _label = new Label
        {
            VerticalOptions = LayoutOptions.Center,
        };
        _label.SetDynamicResource(VisualElement.StyleProperty, "ConnectionStatus");

        _frame = new Border
        {
            Padding = new Thickness(9, 4),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
            Content = new HorizontalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                Children = { _dot, _label },
            },
        };

        Content = _frame;
        Apply();
    }

    public bool IsOnline
    {
        get => (bool)GetValue(IsOnlineProperty);
        set => SetValue(IsOnlineProperty, value);
    }

    private void Apply()
    {
        bool online = IsOnline;
        Color accent = ThemeTokens.Get(online ? "Success" : "Danger");
        Color text = ThemeTokens.Get(online ? "SuccessText" : "Danger");
        _frame.BackgroundColor = ThemeTokens.Get(online ? "SuccessSurface" : "DangerSurface");
        _dot.Color = accent;
        _label.TextColor = text;
        _label.Text = online ? "live" : "offline";
        SemanticProperties.SetDescription(this, online ? "Connection live" : "Connection offline");
    }
}
