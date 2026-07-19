// <copyright file="CoraFab.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Persist;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>
/// Floating Cora assistant (Expo <c>CoraAssistant</c>): gold-ring avatar above the tab bar;
/// tap toggles a speech bubble with <see cref="CoraLines.For"/>. Hidden when prefs disable Cora.
/// </summary>
public sealed class CoraFab : ContentView
{
    public static readonly BindableProperty ScreenKeyProperty = BindableProperty.Create(
        nameof(ScreenKey),
        typeof(string),
        typeof(CoraFab),
        "home",
        propertyChanged: (b, _, _) => ((CoraFab)b).RefreshLine());

    private readonly Label _lineLabel;
    private readonly Border _bubble;
    private readonly VerticalStackLayout _root;
    private Page? _page;
    private bool _open;

    public CoraFab()
    {
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(0, 0, 16, 72);
        InputTransparent = false;
        ZIndex = 20;

        _lineLabel = new Label
        {
            FontFamily = "ManropeRegular",
            FontSize = 13,
            TextColor = ThemeTokens.Get("CoraLine"),
            LineBreakMode = LineBreakMode.WordWrap,
        };

        var eyebrow = new Label
        {
            Text = "CORA BYTE",
            FontFamily = "SpaceMonoRegular",
            FontSize = 10,
            CharacterSpacing = 1,
            TextColor = ThemeTokens.Get("Gold"),
        };

        _bubble = new Border
        {
            IsVisible = false,
            MaximumWidthRequest = 240,
            Padding = new Thickness(14),
            BackgroundColor = ThemeTokens.Get("DeepPurple"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { eyebrow, _lineLabel },
            },
        };

        var avatar = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            BackgroundColor = Color.FromArgb("#1C1430"),
            Stroke = new SolidColorBrush(ThemeTokens.Get("Gold")),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28) },
            HorizontalOptions = LayoutOptions.End,
            Content = new Label
            {
                Text = "C",
                FontFamily = "SpaceGroteskBold",
                FontSize = 22,
                TextColor = ThemeTokens.Get("Gold"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            _open = !_open;
            _bubble.IsVisible = _open;
            RefreshLine();
        };
        avatar.GestureRecognizers.Add(tap);

        _root = new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.End,
            Children = { _bubble, avatar },
        };

        Content = _root;
        RefreshLine();
        Loaded += async (_, _) => await RefreshVisibilityAsync().ConfigureAwait(true);
    }

    public string ScreenKey
    {
        get => (string)GetValue(ScreenKeyProperty);
        set => SetValue(ScreenKeyProperty, value);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (_page is not null)
        {
            _page.Appearing -= OnPageAppearing;
        }

        _page = FindParentPage();
        if (_page is not null)
        {
            _page.Appearing += OnPageAppearing;
        }
    }

    private void OnPageAppearing(object? sender, EventArgs e)
        => _ = RefreshVisibilityAsync();

    private void RefreshLine()
        => _lineLabel.Text = CoraLines.For(ScreenKey);

    private async Task RefreshVisibilityAsync()
    {
        try
        {
            var prefs = Handler?.MauiContext?.Services.GetService<IPrefsStore>();
            if (prefs is null)
            {
                return;
            }

            var p = await prefs.LoadAsync().ConfigureAwait(true);
            IsVisible = p.CoraEnabled;
            if (!p.CoraEnabled)
            {
                _open = false;
                _bubble.IsVisible = false;
            }
        }
        catch
        {
            // Prefs best-effort; keep prior visibility.
        }
    }

    private Page? FindParentPage()
    {
        Element? walk = this;
        while (walk is not null)
        {
            if (walk is Page page)
            {
                return page;
            }

            walk = walk.Parent;
        }

        return null;
    }
}
