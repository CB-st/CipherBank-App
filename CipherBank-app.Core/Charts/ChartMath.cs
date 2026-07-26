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
    public static ChartPathResult ToPath(
        IReadOnlyList<ChartPoint> series,
        double w,
        double h,
        double pad = 6,
        double? min = null,
        double? max = null)
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
        if (dx == 0)
        {
            dx = 1;
        }

        double dy = hi - lo;
        if (dy == 0)
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

        double bas = series[0].V == 0 ? 1 : series[0].V;
        return series.Select(p => new ChartPoint(p.T, ((p.V / bas) - 1) * 100)).ToList();
    }
}
