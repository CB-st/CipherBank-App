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

    [Fact]
    public void SpringStep_Underdamped_ConvergesToTarget()
    {
        var end = RunSpring(0, 0, target: 1, zeta: 0.75, omega: 12, steps: 600, out _);

        end.Position.Should().BeApproximately(1.0, 0.001);
        end.Velocity.Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void SpringStep_Underdamped_OvershootsTarget()
    {
        RunSpring(0, 0, target: 1, zeta: 0.6, omega: 12, steps: 600, out var maxPosition);

        maxPosition.Should().BeGreaterThan(1.0); // a single overshoot past the target
    }

    [Fact]
    public void SpringStep_CriticallyDamped_DoesNotOvershoot()
    {
        RunSpring(0, 0, target: 1, zeta: 1.0, omega: 12, steps: 600, out var maxPosition);

        maxPosition.Should().BeLessThanOrEqualTo(1.0001); // no meaningful overshoot
    }

    [Fact]
    public void SpringStep_RespectsSeedVelocity()
    {
        var first = CarouselMath.SpringStep(0, velocity: 4, target: 1, dt: 1.0 / 60.0, dampingRatio: 0.75, angularFrequency: 12);

        first.Position.Should().BeGreaterThan(0); // moves in the seeded direction immediately
    }

    private static SpringState RunSpring(
        double start,
        double startVelocity,
        double target,
        double zeta,
        double omega,
        int steps,
        out double maxPosition)
    {
        var state = new SpringState(start, startVelocity);
        maxPosition = start;
        for (var i = 0; i < steps; i++)
        {
            state = CarouselMath.SpringStep(state.Position, state.Velocity, target, 1.0 / 60.0, zeta, omega);
            maxPosition = Math.Max(maxPosition, state.Position);
        }

        return state;
    }
}
