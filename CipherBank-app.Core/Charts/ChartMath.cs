// <copyright file="ChartMath.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Numerics;

namespace CipherBank_app.Charts;

/// <summary>Pure chart math ported from Cora chartMath.ts.</summary>
public static class ChartMath
{
    private const double DefaultPad = 6;
    private const double PercentScale = 100;

    /// <summary>
    /// Builds SVG line/area path data for a series using optional padding and value bounds.
    /// Use: High (sparkline layout). Scope: ChartMath path builders.
    /// </summary>
    /// <param name="series">Time/value points in display order.</param>
    /// <param name="width">Output width in device-independent pixels.</param>
    /// <param name="height">Output height in device-independent pixels.</param>
    /// <param name="padding">Vertical padding in device-independent pixels.</param>
    /// <param name="min">Optional fixed minimum value; otherwise inferred from <paramref name="series"/>.</param>
    /// <param name="max">Optional fixed maximum value; otherwise inferred from <paramref name="series"/>.</param>
    public static ChartPathResult ToPath(
        IReadOnlyCollection<ChartPoint> series,
        double width,
        double height,
        double padding = DefaultPad,
        double? min = null,
        double? max = null)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        if (padding * 2 >= height)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Padding must leave a positive drawable height.");
        }

        if (series.Count < 2)
        {
            return new ChartPathResult();
        }

        var x0 = series.Min(p => p.T);
        var x1 = series.Max(p => p.T);
        var lo = min ?? series.Min(p => p.V);
        var hi = max ?? series.Max(p => p.V);
        var dx = x1 - x0;
        var dy = hi - lo;

        // A zero span has no scale. Center that dimension instead of substituting
        // an arbitrary span of 1, which would make near-zero and zero inputs jump.
        var points = series.Select(point =>
        {
            var x = dx == 0 ? width / 2 : ((point.T - x0) / dx) * width;
            var y = dy == 0
                ? height / 2
                : (height - padding) - (((point.V - lo) / dy) * (height - (padding * 2)));
            return new Vector2((float)x, (float)y);
        }).ToList();

        var line = string.Join(
            " ",
            points.Select((point, index) => string.Create(
                CultureInfo.InvariantCulture,
                $"{(index == 0 ? "M" : "L")}{point.X:0.0} {point.Y:0.0}")));
        var area = string.Create(
            CultureInfo.InvariantCulture,
            $"{line} L{width:0.0} {height:0.0} L0 {height:0.0} Z");
        return new ChartPathResult { Line = line, Area = area, Points = points };
    }

    public static IReadOnlyList<ChartPoint> ToIndexed(IReadOnlyList<ChartPoint> series)
    {
        if (series.Count == 0)
        {
            return series;
        }

        var baseline = series[0].V;
        if (baseline == 0)
        {
            throw new InvalidOperationException("A percent-change series requires a non-zero baseline.");
        }

        return series.Select(point => new ChartPoint(
            point.T,
            ((point.V / baseline) - 1) * PercentScale)).ToList();
    }
}
