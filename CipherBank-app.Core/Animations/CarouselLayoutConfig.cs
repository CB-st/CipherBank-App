// <copyright file="CarouselLayoutConfig.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Animations;

/// <summary>
/// Tunable constants for the arc carousel layout.
/// </summary>
public sealed record CarouselLayoutConfig
{
    /// <summary>Horizontal spacing (device-independent px) between adjacent cards.</summary>
    public double Stride { get; init; } = 220;

    /// <summary>Maximum 3D tilt in degrees applied to off-center cards.</summary>
    public double MaxTilt { get; init; } = 45;

    /// <summary>Scale reduction per unit of distance from center.</summary>
    public double ScaleFalloff { get; init; } = 0.18;

    /// <summary>Minimum scale for far cards.</summary>
    public double MinScale { get; init; } = 0.82;

    /// <summary>Opacity reduction per unit of distance from center.</summary>
    public double OpacityFalloff { get; init; } = 0.35;

    /// <summary>Minimum opacity for far cards.</summary>
    public double MinOpacity { get; init; } = 0.4;

    /// <summary>Downward arc drop, applied as ArcDrop * distance^2.</summary>
    public double ArcDrop { get; init; } = 28;

    /// <summary>Spacing multiplier applied beyond the first neighbor (compresses far cards).</summary>
    public double EdgeCompression { get; init; } = 0.6;

    /// <summary>Shared default instance.</summary>
    public static CarouselLayoutConfig Default { get; } = new();
}
