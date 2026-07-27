// <copyright file="ChartPathResult.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Charts;

/// <summary>Result of mapping a series into a drawable path within a box.</summary>
public sealed class ChartPathResult
{
    public string Line { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public IReadOnlyList<(double X, double Y)> Pts { get; init; } = Array.Empty<(double, double)>();
}
