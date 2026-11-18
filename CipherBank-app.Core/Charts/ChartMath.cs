// <copyright file="ChartMath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using Microsoft.Maui.Graphics;

namespace CipherBank_app.Charts;

/// <summary>Pure chart math ported from Cora chartMath.ts.</summary>
public static class ChartMath
{
    private const double DefaultPad = 6;
    private const double PercentScale = 100;
    private const int PaddingSides = 2;

    /// <summary>
    /// Builds SVG line/area path data for a series using the default padding.
    /// Use: High (sparkline layout). Scope: ChartMath path builders.
    /// </summary>
    /// <param name="series">Samples in display order: <see cref="Point.X"/> is time, <see cref="Point.Y"/> is value.</param>
    /// <param name="width">Output width in device-independent pixels.</param>
    /// <param name="height">Output height in device-independent pixels.</param>
    public static ChartPathResult ToPath(
        IReadOnlyCollection<Point> series,
        double width,
        double height)
        => ToPath(series, width, height, DefaultPad);

    /// <summary>
    /// Builds SVG line/area path data for a series using explicit padding and inferred value bounds.
    /// Automatically determines chart/point spacing from the series range.
    /// </summary>
    /// <param name="series">Samples in display order: <see cref="Point.X"/> is time, <see cref="Point.Y"/> is value.</param>
    /// <param name="width">Output width in device-independent pixels.</param>
    /// <param name="height">Output height in device-independent pixels.</param>
    /// <param name="padding">Vertical padding in device-independent pixels.</param>
    public static ChartPathResult ToPath(
        IReadOnlyCollection<Point> series,
        double width,
        double height,
        double padding)
        => ToPathCore(series, width, height, padding, min: null, max: null);

    /// <summary>
    /// Builds SVG line/area path data for a series using explicit padding and value bounds.
    /// </summary>
    /// <param name="series">Samples in display order: <see cref="Point.X"/> is time, <see cref="Point.Y"/> is value.</param>
    /// <param name="width">Output width in device-independent pixels.</param>
    /// <param name="height">Output height in device-independent pixels.</param>
    /// <param name="padding">Vertical padding in device-independent pixels.</param>
    /// <param name="min">Fixed minimum value for the vertical scale.</param>
    /// <param name="max">Fixed maximum value for the vertical scale.</param>
    public static ChartPathResult ToPath(
        IReadOnlyCollection<Point> series,
        double width,
        double height,
        double padding,
        double min,
        double max)
        => ToPathCore(series, width, height, padding, min, max);

    /// <summary>
    /// Converts absolute values into percent-change from the first point (index 0 baseline).
    /// </summary>
    /// <param name="series">Absolute value series; first non-empty point is the baseline.</param>
    public static IReadOnlyList<Point> ToIndexed(IReadOnlyList<Point> series)
    {
        if (series.Count == 0)
        {
            return series;
        }

        double baseline = series[0].Y;
        if (baseline == 0)
        {
            throw new InvalidOperationException(
                $"A percent-change conversion requires a non-zero baseline in {nameof(series)}.");
        }

        return series.Select(point => new Point(
            point.X,
            ((point.Y / baseline) - 1) * PercentScale)).ToList();
    }

    private static ChartPathResult ToPathCore(
        IReadOnlyCollection<Point> series,
        double width,
        double height,
        double padding,
        double? min,
        double? max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);
        if (padding * PaddingSides >= height)
        {
            throw new ArgumentOutOfRangeException(nameof(padding), "Padding must leave a positive drawable height.");
        }

        if (series.Count < 2)
        {
            return new ChartPathResult();
        }

        double x0 = series.Min(p => p.X);
        double x1 = series.Max(p => p.X);
        double lo = min ?? series.Min(p => p.Y);
        double hi = max ?? series.Max(p => p.Y);
        double dx = x1 - x0;
        double dy = hi - lo;

        // A zero span has no scale. Center that dimension instead of substituting
        // an arbitrary span of 1, which would make near-zero and zero inputs jump.
        List<PointF> points = series.Select(point =>
        {
            double x = dx == 0 ? width / 2 : ((point.X - x0) / dx) * width;
            double y = dy == 0
                ? height / 2
                : (height - padding) - (((point.Y - lo) / dy) * (height - (padding * PaddingSides)));
            return new PointF((float)x, (float)y);
        }).ToList();

        string line = string.Join(
            " ",
            points.Select((point, index) => string.Create(
                CultureInfo.InvariantCulture,
                $"{(index == 0 ? "M" : "L")}{point.X:0.0} {point.Y:0.0}")));
        string area = string.Create(
            CultureInfo.InvariantCulture,
            $"{line} L{width:0.0} {height:0.0} L0 {height:0.0} Z");
        return new ChartPathResult { Line = line, Area = area, Points = points };
    }
}
