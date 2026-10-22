// <copyright file="GlowBackdrop.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Controls;

/// <summary>
/// The vision's radial glow backdrop: soft pastel glows in light mode, deep
/// magenta/indigo glows in dark mode. Place as the first child of a page's root Grid,
/// behind the scrolling content, so glass cards have something to refract.
/// </summary>
public class GlowBackdrop : Grid
{
    public GlowBackdrop()
    {
        InputTransparent = true;
        CascadeInputTransparent = true;

        Children.Add(MakeGlow("GlowPink", "GlowMagentaDark", 420, LayoutOptions.Start, LayoutOptions.Start, -120, -80));
        Children.Add(MakeGlow("GlowLavender", "GlowIndigoDark", 380, LayoutOptions.End, LayoutOptions.Start, 110, 40));
        Children.Add(MakeGlow("GlowPeach", "Tertiary", 360, LayoutOptions.Start, LayoutOptions.End, -60, 100));
    }

    private static Ellipse MakeGlow(
        string lightKey,
        string darkKey,
        double size,
        LayoutOptions horizontal,
        LayoutOptions vertical,
        double translationX,
        double translationY)
    {
        var ellipse = new Ellipse
        {
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = horizontal,
            VerticalOptions = vertical,
            TranslationX = translationX,
            TranslationY = translationY,
            Opacity = 0.55,
            InputTransparent = true,
        };
        ellipse.SetAppTheme<Brush>(
            Shape.FillProperty,
            MakeRadial(ThemeTokens.Get(lightKey)),
            MakeRadial(ThemeTokens.Get(darkKey)));
        return ellipse;
    }

    private static RadialGradientBrush MakeRadial(Color color)
    {
        return new RadialGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(color, 0.0f),
                new GradientStop(color.WithAlpha(0f), 1.0f),
            },
            new Point(0.5, 0.5),
            0.5);
    }
}
