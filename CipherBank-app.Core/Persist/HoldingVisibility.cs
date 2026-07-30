// <copyright file="HoldingVisibility.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.Persist;

/// <summary>Partitions Home holdings by the user's enabled currency preferences.</summary>
public static class HoldingVisibility
{
    /// <summary>Splits holdings into enabled and other assets, using defaults when no symbols are configured.</summary>
    public static HoldingVisibilityResult Split(
        IEnumerable<HoldingDto> holdings,
        IEnumerable<string>? enabledCurrencies)
    {
        var enabled = (enabledCurrencies ?? Array.Empty<string>())
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (enabled.Count == 0)
        {
            enabled.UnionWith(UserPrefs.DefaultEnabledCurrencies);
        }

        var visible = new List<HoldingDto>();
        var other = new List<HoldingDto>();
        foreach (HoldingDto holding in holdings)
        {
            if (enabled.Contains(holding.Symbol))
            {
                visible.Add(holding);
            }
            else
            {
                other.Add(holding);
            }
        }

        return new HoldingVisibilityResult(visible, other);
    }
}
