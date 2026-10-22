// <copyright file="PricePoint.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents a single price point in a price history series.
/// </summary>
public record PricePoint(
    DateTimeOffset Timestamp,
    decimal Price,
    decimal? Volume = null);
