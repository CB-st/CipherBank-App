// <copyright file="AssetSymbolJsonConverter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherBank_app.Models;

/// <summary>
/// Reads and writes <see cref="AssetSymbol"/> values as JSON strings.
/// </summary>
public sealed class AssetSymbolJsonConverter : JsonConverter<AssetSymbol>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override AssetSymbol Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Asset symbol must be a JSON string.");
        }

        string? value = reader.GetString();
        if (!AssetSymbol.TryParse(value, out AssetSymbol? symbol))
        {
            throw new JsonException("Asset symbol must be a nonblank string.");
        }

        return symbol!;
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        AssetSymbol value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}
