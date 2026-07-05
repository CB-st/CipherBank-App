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

    [Theory]
    [InlineData(2.4, 0.0, 2)] // slow: rounds to nearest (down)
    [InlineData(2.6, 0.0, 3)] // slow: rounds to nearest (up)
    [InlineData(2.4, 5.0, 3)] // fast flick right: advances past the floor
    [InlineData(2.6, -5.0, 2)] // fast flick left: retreats below the ceiling
    public void ComputeTargetIndex_PicksExpectedIndex(double position, double velocity, int expected)
    {
        CarouselMath.ComputeTargetIndex(position, velocity, count: 5, flickThreshold: 2.0)
            .Should().Be(expected);
    }

    [Fact]
    public void ComputeTargetIndex_ClampsToCollectionBounds()
    {
        CarouselMath.ComputeTargetIndex(0.1, -10.0, count: 5, flickThreshold: 2.0).Should().Be(0);
        CarouselMath.ComputeTargetIndex(4.0, 10.0, count: 5, flickThreshold: 2.0).Should().Be(4);
    }

    [Fact]
    public void ComputeTargetIndex_VeryFastFlick_SkipsAhead()
    {
        // velocity well above 2x threshold advances more than one step
        CarouselMath.ComputeTargetIndex(1.0, 9.0, count: 10, flickThreshold: 2.0)
            .Should().BeGreaterThan(2);
    }
}
