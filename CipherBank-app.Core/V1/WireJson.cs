// <copyright file="WireJson.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>
/// Shared helpers for folding camelCase JSON aliases into SCREAMING_SNAKE DTO properties via ExtensionData.
/// Use: High (every prefs/bootstrap deserialize). Scope: V1 wire DTOs.
/// </summary>
internal static class WireJson
{
    /// <summary>
    /// Tries to read a string list from extension data under <paramref name="camelName"/>.
    /// Use: High (prefs fold). Scope: WireJson.
    /// </summary>
    internal static List<string>? TryGetStringList(IDictionary<string, JsonElement>? data, string camelName)
    {
        if (data is null || !data.TryGetValue(camelName, out JsonElement el) || el.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return el.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToList();
    }

    /// <summary>
    /// Tries to read a string→bool map from extension data under <paramref name="camelName"/>.
    /// Use: High (prefs fold). Scope: WireJson.
    /// </summary>
    internal static Dictionary<string, bool>? TryGetBoolMap(IDictionary<string, JsonElement>? data, string camelName)
    {
        if (data is null || !data.TryGetValue(camelName, out JsonElement el) || el.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return el.EnumerateObject()
            .Where(prop => prop.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            .ToDictionary(prop => prop.Name, prop => prop.Value.GetBoolean(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Tries to read a string from extension data under <paramref name="camelName"/>.
    /// Use: High (bootstrap fold). Scope: WireJson.
    /// </summary>
    internal static string? TryGetString(IDictionary<string, JsonElement>? data, string camelName)
    {
        if (data is null || !data.TryGetValue(camelName, out JsonElement el) || el.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return el.GetString();
    }

    /// <summary>
    /// Tries to read a bool from extension data under <paramref name="camelName"/>.
    /// Use: High (prefs fold). Scope: WireJson.
    /// </summary>
    internal static bool? TryGetBool(IDictionary<string, JsonElement>? data, string camelName)
    {
        if (data is null || !data.TryGetValue(camelName, out JsonElement el))
        {
            return null;
        }

        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    /// <summary>
    /// Tries to read an Int64 from extension data under <paramref name="camelName"/>.
    /// Use: Medium (bootstrap fold). Scope: WireJson.
    /// </summary>
    internal static long? TryGetInt64(IDictionary<string, JsonElement>? data, string camelName)
    {
        if (data is null || !data.TryGetValue(camelName, out JsonElement el))
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var value) ? value : null;
    }

    /// <summary>
    /// Tries to deserialize a nested object from extension data under <paramref name="camelName"/>.
    /// Use: High (bootstrap prefs). Scope: WireJson.
    /// </summary>
    internal static bool TryDeserializeObject<T>(
        IDictionary<string, JsonElement>? data,
        string camelName,
        out T? value)
        where T : class
    {
        value = null;
        if (data is null || !data.TryGetValue(camelName, out JsonElement el) || el.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        value = el.Deserialize<T>();
        return value is not null;
    }

    /// <summary>
    /// Tries to deserialize a list of nested objects from extension data under <paramref name="camelName"/>.
    /// Use: High (bootstrap recipients). Scope: WireJson.
    /// </summary>
    internal static bool TryDeserializeObjectList<T>(
        IDictionary<string, JsonElement>? data,
        string camelName,
        out List<T>? value)
    {
        value = null;
        if (data is null || !data.TryGetValue(camelName, out JsonElement el) || el.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        value = el.Deserialize<List<T>>();
        return value is not null;
    }
}
