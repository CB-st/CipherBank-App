// <copyright file="ChartMathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Charts;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Charts;

public class ChartMathTests
{
    [Fact]
    public void ToPath_WithTwoPoints_ProducesLineAndArea()
    {
        var series = new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) };
        var result = ChartMath.ToPath(series, 100, 40);
        result.Line.Should().StartWith("M");
        result.Area.Should().EndWith("Z");
        result.Pts.Should().HaveCount(2);
    }

    [Fact]
    public void ToIndexed_NormalizesToPercentChange()
    {
        var series = new[] { new ChartPoint(0, 100), new ChartPoint(1, 110) };
        var indexed = ChartMath.ToIndexed(series);
        indexed[0].V.Should().BeApproximately(0, 0.0001);
        indexed[1].V.Should().BeApproximately(10, 0.0001);
    }

    [Fact]
    public void ToIndexed_Empty_ReturnsEmpty()
    {
        ChartMath.ToIndexed(Array.Empty<ChartPoint>()).Should().BeEmpty();
    }
}
