// <copyright file="ChartPathResult.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Maui.Graphics;

namespace CipherBank_app.Charts;

/// <summary>Result of mapping a series into a drawable path within a box.</summary>
public sealed class ChartPathResult
{
    public string Line { get; init; } = string.Empty;

    public string Area { get; init; } = string.Empty;

    /// <summary>Rendered device-independent pixel coordinates.</summary>
    public IReadOnlyList<PointF> Points { get; init; } = Array.Empty<PointF>();
}
