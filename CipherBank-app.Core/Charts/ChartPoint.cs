// <copyright file="ChartPoint.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Charts;

/// <summary>Point in a time series chart.</summary>
public readonly record struct ChartPoint(double T, double V);
