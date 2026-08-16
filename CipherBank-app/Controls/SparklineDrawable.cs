// <copyright file="SparklineDrawable.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Charts;
using Microsoft.Maui.Graphics;

namespace CipherBank_app.Controls;

/// <summary>GraphicsView drawable for a single sparkline series.</summary>
public sealed class SparklineDrawable : IDrawable
{
    public IReadOnlyList<ChartPoint> Series { get; set; } = Array.Empty<ChartPoint>();

    public Color Stroke { get; set; } = ThemeTokens.Get("Gold");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Series.Count < 2)
        {
            return;
        }

        var path = ChartMath.ToPath(Series, dirtyRect.Width, dirtyRect.Height);
        if (path.Points.Count < 2)
        {
            return;
        }

        PathF geo = new PathF();
        geo.MoveTo(path.Points[0].X, path.Points[0].Y);
        for (int i = 1; i < path.Points.Count; i++)
        {
            geo.LineTo(path.Points[i].X, path.Points[i].Y);
        }

        canvas.StrokeColor = Stroke;
        canvas.StrokeSize = 2;
        canvas.DrawPath(geo);
    }
}
