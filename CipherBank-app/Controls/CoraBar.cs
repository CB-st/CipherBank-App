// <copyright file="CoraBar.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
        "home");

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
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
        };
        _lineLabel.SetDynamicResource(VisualElement.StyleProperty, "CoraLineText");

        Label avatarGlyph = new Label
        {
            Text = "C",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        avatarGlyph.SetDynamicResource(VisualElement.StyleProperty, "CoraAvatarGlyph");

        Border avatar = new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            BackgroundColor = ThemeTokens.Get("CoraAvatarWell"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(21) },
            VerticalOptions = LayoutOptions.Center,
            Content = avatarGlyph,
        };

        Border frame = new Border
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

    /// <summary>Screen key retained for automation and analytics context.</summary>
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
        => _lineLabel.Text = Line ?? string.Empty;

    private async Task RefreshVisibilityAsync()
    {
        try
        {
            var prefs = Handler?.MauiContext?.Services.GetService<IPrefsStore>();
            if (prefs is not null)
            {
                IsVisible = (await prefs.LoadAsync().ConfigureAwait(true)).CoraEnabled;
            }
        }
        catch (Exception)
        {
            // Preference visibility is best-effort at the view boundary.
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
