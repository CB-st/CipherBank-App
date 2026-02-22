using System;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a single price point in a price history series.
/// </summary>
/// <param name="Timestamp">When this price was recorded.</param>
/// <param name="Price">The price at this point in time.</param>
/// <param name="Volume">The trading volume at this point (optional).</param>
public record PricePoint(
    DateTimeOffset Timestamp,
    decimal Price,
    decimal? Volume = null);
