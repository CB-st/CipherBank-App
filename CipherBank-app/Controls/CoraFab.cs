// <copyright file="CoraFab.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>
/// Floating Cora assistant (Expo <c>CoraAssistant</c>): gold-ring avatar above the tab bar;
/// tap toggles a speech bubble whose copy is supplied by the owning ViewModel.
/// </summary>
public sealed class CoraFab : ContentView
{
    public static readonly BindableProperty ScreenKeyProperty = BindableProperty.Create(
        nameof(ScreenKey),
        typeof(string),
        typeof(CoraFab),
        "home");

    public static readonly BindableProperty LineProperty = BindableProperty.Create(
        nameof(Line),
        typeof(string),
        typeof(CoraFab),
        defaultValue: null,
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
        AutomationId = "CoraFab";
        SemanticProperties.SetDescription(this, "Cora assistant");

        _lineLabel = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap,
        };
        _lineLabel.SetDynamicResource(VisualElement.StyleProperty, "CoraLineText");

        Label eyebrow = new Label
        {
            Text = "CORA BYTE",
        };
        eyebrow.SetDynamicResource(VisualElement.StyleProperty, "CoraEyebrow");

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

        Label avatarGlyph = new Label
        {
            Text = "C",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        avatarGlyph.SetDynamicResource(VisualElement.StyleProperty, "CoraFabGlyph");

        Border avatar = new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            BackgroundColor = ThemeTokens.Get("WelcomeFrame"),
            Stroke = new SolidColorBrush(ThemeTokens.Get("Gold")),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(28) },
            HorizontalOptions = LayoutOptions.End,
            Content = avatarGlyph,
        };

        TapGestureRecognizer tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            _open = !_open;
            _bubble.IsVisible = _open;
            RefreshLine();
            SemanticProperties.SetDescription(
                this,
                _open ? "Cora assistant open" : "Cora assistant");
        };
        avatar.GestureRecognizers.Add(tap);
        AutomationProperties.SetIsInAccessibleTree(avatar, true);
        SemanticProperties.SetDescription(avatar, "Toggle Cora assistant");


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

    /// <summary>Gets or sets the line supplied by the page ViewModel.</summary>
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
            if (prefs is null)
            {
                return;
            }

            IsVisible = (await prefs.LoadAsync().ConfigureAwait(true)).CoraEnabled;
            if (!IsVisible)
            {
                _open = false;
                _bubble.IsVisible = false;
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
