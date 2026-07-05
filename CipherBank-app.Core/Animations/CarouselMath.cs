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

    /// <summary>
    /// Chooses the card index to settle on after a drag, given the release velocity.
    /// Below the flick threshold it snaps to the nearest index; above it, it advances one
    /// or more steps in the flick direction.
    /// </summary>
    public static int ComputeTargetIndex(double position, double velocity, int count, double flickThreshold)
    {
        if (count <= 0)
        {
            return 0;
        }

        int target;
        if (Math.Abs(velocity) >= flickThreshold)
        {
            int direction = velocity > 0 ? 1 : -1;
            target = direction > 0
                ? (int)Math.Floor(position) + 1
                : (int)Math.Ceiling(position) - 1;

            int extra = (int)((Math.Abs(velocity) - flickThreshold) / (flickThreshold * 2.0));
            target += direction * extra;
        }
        else
        {
            target = (int)Math.Round(position, MidpointRounding.AwayFromZero);
        }

        return Math.Clamp(target, 0, count - 1);
    }
}
