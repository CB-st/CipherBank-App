// <copyright file="GlassCard.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>
/// A glassmorphism panel: real native blur on iOS/Mac Catalyst, simulated translucent
/// glass elsewhere. Child content declared in XAML lands in <see cref="Body"/>.
/// </summary>
[ContentProperty(nameof(Body))]
public class GlassCard : ContentView
{
    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius),
        typeof(double),
        typeof(GlassCard),
        20.0,
        propertyChanged: (bindable, _, _) => ((GlassCard)bindable).UpdateCornerRadius());

    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body),
        typeof(View),
        typeof(GlassCard),
        propertyChanged: (bindable, _, newValue) => ((GlassCard)bindable)._bodyHost.Content = (View?)newValue);

    public static readonly BindableProperty BodyPaddingProperty = BindableProperty.Create(
        nameof(BodyPadding),
        typeof(Thickness),
        typeof(GlassCard),
        new Thickness(20),
        propertyChanged: (bindable, _, newValue) => ((GlassCard)bindable)._bodyHost.Padding = (Thickness)newValue);

    private readonly Border _frame;
    private readonly ContentView _bodyHost;
#if IOS || MACCATALYST
    private readonly BlurBackdropView _blur;
#endif

    public GlassCard()
    {
        _bodyHost = new ContentView { Padding = new Thickness(20) };

        var tint = new BoxView { InputTransparent = true };
        var layers = new Grid();

#if IOS || MACCATALYST
        _blur = new BlurBackdropView { InputTransparent = true };
        layers.Children.Add(_blur);
        tint.SetAppThemeColor(
            BoxView.ColorProperty,
            ThemeTokens.Get("Surface").WithAlpha(0.40f),
            ThemeTokens.Get("SurfaceDark").WithAlpha(0.35f));
#else
        // Simulated glass where no native blur is available.
        tint.SetAppThemeColor(
            BoxView.ColorProperty,
            ThemeTokens.Get("Surface").WithAlpha(0.92f),
            ThemeTokens.Get("SurfaceDark").WithAlpha(0.80f));
#endif
        layers.Children.Add(tint);
        layers.Children.Add(_bodyHost);

        _frame = new Border
        {
            StrokeThickness = 1,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
            Content = layers,
            Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 8), Radius = 24, Opacity = 0.18f },
        };
        _frame.SetAppTheme<Brush>(
            Border.StrokeProperty,
            new SolidColorBrush(ThemeTokens.Get("Hairline")),
            new SolidColorBrush(ThemeTokens.Get("HairlineDark")));

        Content = _frame;

#if IOS || MACCATALYST
        ApplyBlurMaterial();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
#endif
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public Thickness BodyPadding
    {
        get => (Thickness)GetValue(BodyPaddingProperty);
        set => SetValue(BodyPaddingProperty, value);
    }

    private void UpdateCornerRadius() =>
        _frame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) };

#if IOS || MACCATALYST
    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }

        ApplyBlurMaterial();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyBlurMaterial();

    private void ApplyBlurMaterial() =>
        _blur.UseDarkMaterial = (Application.Current?.RequestedTheme ?? AppTheme.Light) == AppTheme.Dark;
#endif
}
