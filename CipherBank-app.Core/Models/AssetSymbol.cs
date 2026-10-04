// <copyright file="AssetSymbol.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CipherBank_app.Models;

/// <summary>
/// Immutable, open-set asset ticker normalized for application identity.
/// </summary>
[JsonConverter(typeof(AssetSymbolJsonConverter))]
public sealed record AssetSymbol
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetSymbol"/> class.
    /// </summary>
    /// <param name="value">Ticker text to trim and uppercase invariant.</param>
    public AssetSymbol(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToUpperInvariant();
    }

    /// <summary>Gets the normalized ticker value.</summary>
    public string Value { get; }

    /// <summary>Converts ticker text to a normalized symbol.</summary>
    /// <param name="value">Ticker text.</param>
    public static implicit operator AssetSymbol(string value) => new(value);

    /// <summary>Parses ticker text into a normalized symbol.</summary>
    /// <param name="value">Ticker text.</param>
    /// <returns>The normalized symbol.</returns>
    public static AssetSymbol Parse(string value) => new(value);

    /// <summary>Attempts to parse ticker text into a normalized symbol.</summary>
    /// <param name="value">Ticker text.</param>
    /// <param name="symbol">The normalized symbol when successful.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is nonblank.</returns>
    public static bool TryParse(
        string? value,
        [NotNullWhen(true)] out AssetSymbol? symbol)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            symbol = null;
            return false;
        }

        symbol = new AssetSymbol(value);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
