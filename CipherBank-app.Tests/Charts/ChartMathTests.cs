// <copyright file="ChartMathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;

using CipherBank_app.Charts;
using FluentAssertions;
using Microsoft.Maui.Graphics;
using Xunit;

namespace CipherBank_app.Tests.Charts;

public class ChartMathTests
{
    [Fact]
    public void ToPath_WithTwoPoints_ProducesLineAndArea()
    {
        Point[] series = new[] { new Point(0, 1), new Point(1, 2) };
        ChartPathResult result = ChartMath.ToPath(series, 100, 40);
        result.Line.Should().StartWith("M");
        result.Area.Should().EndWith("Z");
        result.Points.Should().HaveCount(2);
    }

    [Fact]
    public void ToPath_FormatsCoordinatesWithInvariantCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Point[] series = new[] { new Point(0, 1.5), new Point(1, 2.5) };
            ChartPathResult result = ChartMath.ToPath(series, 100, 40);
            result.Line.Should().NotContain(",");
            result.Area.Should().NotContain(",");
            result.Line.Should().MatchRegex(@"M[\d.]+ [\d.]+");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ToIndexed_NormalizesToPercentChange()
    {
        Point[] series = new[] { new Point(0, 100), new Point(1, 110) };
        IReadOnlyList<Point> indexed = ChartMath.ToIndexed(series);
        indexed[0].Y.Should().BeApproximately(0, 0.0001);
        indexed[1].Y.Should().BeApproximately(10, 0.0001);
    }

    [Fact]
    public void ToIndexed_Empty_ReturnsEmpty() => ChartMath.ToIndexed(Array.Empty<Point>()).Should().BeEmpty();

    [Fact]
    public void ToPath_ZeroSpans_CentersCoordinates()
    {
        Point[] series = [new(1, 5), new(1, 5)];

        ChartPathResult result = ChartMath.ToPath(series, 100, 40);

        result.Points.Should().OnlyContain(point => point.X == 50 && point.Y == 20);
    }

    [Fact]
    public void ToIndexed_ZeroBaseline_RejectsUndefinedPercentChange()
    {
        Action act = () => ChartMath.ToIndexed([new Point(0, 0), new Point(1, 2)]);

        act.Should().Throw<InvalidOperationException>();
    }
}
