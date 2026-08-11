// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>A cached market rate.</summary>
public sealed record RateRow(string Symbol, double Usd, double Change24h, long UpdatedAtMs);
