// <copyright file="CarouselMathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Animations;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Animations;

public class CarouselMathTests
{
    private static readonly CarouselLayoutConfig Config = CarouselLayoutConfig.Default;

    [Fact]
    public void ComputeCardTransform_AtCenter_IsNeutral()
    {
        var t = CarouselMath.ComputeCardTransform(0, Config);

        t.TranslationX.Should().Be(0);
        t.TranslationY.Should().Be(0);
        t.RotationY.Should().Be(0);
        t.Scale.Should().Be(1);
        t.Opacity.Should().Be(1);
        t.ZIndex.Should().Be(0);
    }

    [Fact]
    public void ComputeCardTransform_RightNeighbor_TiltsAndRecedes()
    {
        var t = CarouselMath.ComputeCardTransform(1, Config);

        t.TranslationX.Should().BeApproximately(Config.Stride, 0.0001);
        t.RotationY.Should().Be(-Config.MaxTilt);
        t.Scale.Should().BeApproximately(0.82, 0.0001);
        t.ZIndex.Should().BeLessThan(0);
    }

    [Fact]
    public void ComputeCardTransform_FarCard_ClampsScaleAndOpacityAndTilt()
    {
        var t = CarouselMath.ComputeCardTransform(10, Config);

        t.Scale.Should().Be(Config.MinScale);
        t.Opacity.Should().Be(Config.MinOpacity);
        t.RotationY.Should().Be(-Config.MaxTilt);
    }

    [Fact]
    public void ComputeCardTransform_IsSymmetricInTiltDirection()
    {
        var left = CarouselMath.ComputeCardTransform(-1, Config);
        var right = CarouselMath.ComputeCardTransform(1, Config);

        left.RotationY.Should().Be(-right.RotationY);
        left.TranslationX.Should().Be(-right.TranslationX);
    }
}
