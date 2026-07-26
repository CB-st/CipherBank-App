// <copyright file="ChartMath.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Charts;

/// <summary>Point in a time series chart.</summary>
public readonly record struct ChartPoint(double T, double V);

/// <summary>Result of mapping a series into a drawable path within a box.</summary>
public sealed class ChartPathResult
{
    public string Line { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public IReadOnlyList<(double X, double Y)> Pts { get; init; } = Array.Empty<(double, double)>();
}

/// <summary>Pure chart math ported from Cora chartMath.ts.</summary>
public static class ChartMath
{
    private const double DefaultPad = 6;
    private const double Epsilon = 1e-12;

    public static ChartPathResult ToPath(IReadOnlyList<ChartPoint> series, double w, double h)
        => ToPath(series, w, h, DefaultPad, min: null, max: null);

    public static ChartPathResult ToPath(IReadOnlyList<ChartPoint> series, double w, double h, double pad)
        => ToPath(series, w, h, pad, min: null, max: null);

    public static ChartPathResult ToPath(
        IReadOnlyList<ChartPoint> series,
        double w,
        double h,
        double pad,
        double? min,
        double? max)
    {
        if (series.Count < 2)
        {
            return new ChartPathResult();
        }

        double x0 = series.Min(p => p.T);
        double x1 = series.Max(p => p.T);
        double lo = min ?? series.Min(p => p.V);
        double hi = max ?? series.Max(p => p.V);
        double dx = x1 - x0;
        if (NearlyZero(dx))
        {
            dx = 1;
        }

        double dy = hi - lo;
        if (NearlyZero(dy))
        {
            dy = 1;
        }

        var pts = series.Select(p =>
        {
            double x = ((p.T - x0) / dx) * w;
            double y = h - pad - ((p.V - lo) / dy) * (h - (pad * 2));
            return (x, y);
        }).ToList();

        var lineParts = new List<string>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            string cmd = i == 0 ? "M" : "L";
            lineParts.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{cmd}{pts[i].x:0.0} {pts[i].y:0.0}"));
        }

        string line = string.Join(" ", lineParts);
        string area = string.Create(
            CultureInfo.InvariantCulture,
            $"{line} L{w:0.0} {h:0.0} L0 {h:0.0} Z");
        return new ChartPathResult { Line = line, Area = area, Pts = pts };
    }

    public static IReadOnlyList<ChartPoint> ToIndexed(IReadOnlyList<ChartPoint> series)
    {
        if (series.Count == 0)
        {
            return series;
        }

        double bas = NearlyZero(series[0].V) ? 1 : series[0].V;
        return series.Select(p => new ChartPoint(p.T, ((p.V / bas) - 1) * 100)).ToList();
    }

    /// <summary>
    /// True when a chart span is effectively zero and must be substituted to avoid divide-by-zero.
    /// Use: High (path layout). Scope: ChartMath.
    /// </summary>
    private static bool NearlyZero(double value) => Math.Abs(value) < Epsilon;
}
