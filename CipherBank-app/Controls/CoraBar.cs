// <copyright file="CoraBar.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Persist;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>
/// Quiet inline Cora strip (Expo <c>CoraBar</c>): deep-purple row with avatar slot + one dry line.
/// Hidden when prefs disable Cora. Pair with <see cref="CoraFab"/> on money screens.
/// </summary>
public sealed class CoraBar : ContentView
{
    public static readonly BindableProperty ScreenKeyProperty = BindableProperty.Create(
        nameof(ScreenKey),
        typeof(string),
        typeof(CoraBar),
        "home",
        propertyChanged: (b, _, _) => ((CoraBar)b).RefreshLine());

    public static readonly BindableProperty LineProperty = BindableProperty.Create(
        nameof(Line),
        typeof(string),
        typeof(CoraBar),
        defaultValue: null,
        propertyChanged: (b, _, _) => ((CoraBar)b).RefreshLine());

    private readonly Label _lineLabel;
    private Page? _page;

    public CoraBar()
    {
        AutomationId = "CoraBar";

        _lineLabel = new Label
        {
            FontFamily = "ManropeRegular",
            FontSize = 13,
            TextColor = Color.FromArgb("#E9E4F2"),
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
        };

        var avatar = new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            BackgroundColor = Color.FromArgb("#4A3D63"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(21) },
            VerticalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = "C",
                FontFamily = "SpaceGroteskBold",
                FontSize = 16,
                TextColor = ThemeTokens.Get("Gold"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        };

        var frame = new Border
        {
            Padding = new Thickness(10),
            BackgroundColor = ThemeTokens.Get("DeepPurple"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18) },
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Auto),
                    new(GridLength.Star),
                },
                ColumnSpacing = 11,
                Children = { avatar, _lineLabel },
            },
        };
        Grid.SetColumn(_lineLabel, 1);

        Content = frame;
        RefreshLine();
        Loaded += async (_, _) => await RefreshVisibilityAsync().ConfigureAwait(true);
        SemanticProperties.SetDescription(this, "Cora assistant line");
    }

    /// <summary>Screen key used with <see cref="CoraLines.For"/> when <see cref="Line"/> is empty.</summary>
    public string ScreenKey
    {
        get => (string)GetValue(ScreenKeyProperty);
        set => SetValue(ScreenKeyProperty, value);
    }

    /// <summary>Optional explicit line (e.g. bound from ViewModel <c>CoraLine</c>).</summary>
    public string? Line
    {
        get => (string?)GetValue(LineProperty);
        set => SetValue(LineProperty, value);
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
    {
        string? explicitLine = Line;
        _lineLabel.Text = string.IsNullOrWhiteSpace(explicitLine)
            ? CoraLines.For(ScreenKey)
            : explicitLine;
    }

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
        }
        catch
        {
            // Prefs best-effort.
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
