// <copyright file="ChartSeries.cs" company="CipherBank">
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

    public Color Stroke { get; set; } = ThemeTokens.Get("Gold");
}
