// <copyright file="CompareChartDrawable.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Charts;
using Microsoft.Maui.Graphics;

namespace CipherBank_app.Controls;

/// <summary>One labeled series for the compare chart.</summary>
public sealed class ChartSeries
{
    public string Label { get; set; } = string.Empty;

    public IReadOnlyList<ChartPoint> Points { get; set; } = Array.Empty<ChartPoint>();

    public Color Stroke { get; set; } = Colors.DodgerBlue;
}

/// <summary>Multi-series % change overlay (Cora CompareChart).</summary>
public sealed class CompareChartDrawable : IDrawable
{
    private static readonly Color[] DefaultColors =
    {
        Color.FromArgb("#2B59FF"),
        Color.FromArgb("#22C55E"),
        Color.FromArgb("#F59E0B"),
        Color.FromArgb("#EC4899"),
    };

    public IReadOnlyList<ChartSeries> Series { get; set; } = Array.Empty<ChartSeries>();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Series.Count == 0)
        {
            return;
        }

        var indexed = Series.Select(s =>
        {
            var pts = ChartMath.ToIndexed(s.Points);
            return (s.Label, Pts: pts, Stroke: s.Stroke);
        }).ToList();

        var all = indexed.SelectMany(s => s.Pts).Select(p => p.V).ToList();
        if (all.Count < 2)
        {
            return;
        }

        double lo = Math.Min(all.Min(), 0);
        double hi = Math.Max(all.Max(), 0);
        for (int i = 0; i < indexed.Count; i++)
        {
            var pathResult = ChartMath.ToPath(indexed[i].Pts, dirtyRect.Width, dirtyRect.Height, 10, lo, hi);
            if (pathResult.Pts.Count < 2)
            {
                continue;
            }

            var geo = new PathF();
            geo.MoveTo((float)pathResult.Pts[0].X, (float)pathResult.Pts[0].Y);
            for (int j = 1; j < pathResult.Pts.Count; j++)
            {
                geo.LineTo((float)pathResult.Pts[j].X, (float)pathResult.Pts[j].Y);
            }

            canvas.StrokeColor = indexed[i].Stroke.Alpha > 0 ? indexed[i].Stroke : DefaultColors[i % DefaultColors.Length];
            canvas.StrokeSize = 2.2f;
            canvas.DrawPath(geo);
        }
    }
}
