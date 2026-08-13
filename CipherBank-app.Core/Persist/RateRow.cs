// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>A cached market rate.</summary>
public sealed record RateRow(string Symbol, double Usd, double Change24h, long UpdatedAtMs);
