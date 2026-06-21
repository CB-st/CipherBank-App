// <copyright file="CarouselMath.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Animations;

/// <summary>
/// Pure, UI-agnostic math for the arc wallet carousel.
/// </summary>
public static class CarouselMath
{
    /// <summary>
    /// Computes a card's transform from its signed distance to the centered position.
    /// </summary>
    public static CardTransform ComputeCardTransform(double distance, CarouselLayoutConfig config)
    {
        double abs = Math.Abs(distance);
        double sign = Math.Sign(distance);
        double spread = Math.Min(abs, 1.0) + (config.EdgeCompression * Math.Max(abs - 1.0, 0.0));

        double translationX = sign * config.Stride * spread;
        double translationY = config.ArcDrop * distance * distance;
        double rotationY = Math.Clamp(-distance * config.MaxTilt, -config.MaxTilt, config.MaxTilt);
        double scale = Math.Max(config.MinScale, 1.0 - (config.ScaleFalloff * abs));
        double opacity = Math.Max(config.MinOpacity, 1.0 - (config.OpacityFalloff * abs));
        int zIndex = -(int)Math.Round(abs * 100, MidpointRounding.AwayFromZero);

        return new CardTransform(translationX, translationY, rotationY, scale, opacity, zIndex);
    }
}
