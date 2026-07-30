// <copyright file="UserPrefs.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;

namespace CipherBank_app.Persist;

/// <summary>User preference model (Cora prefs).</summary>
public sealed class UserPrefs
{
    public static readonly string[] DefaultHomeOrder =
    {
        "cora", "balance", "quickActions", "performance", "holdings", "localWallets",
    };

    public static readonly string[] DefaultEnabledCurrencies = { "BTC", "XMR", "USD" };

    public static string SectionHoldings { get; } = "holdings";

    public static string SectionLocalWallets { get; } = "localWallets";

    public static string SectionLegacyAssets { get; } = "assets";

    public Collection<string> HomeOrder { get; set; } = new(DefaultHomeOrder.ToList());

    public Dictionary<string, bool> HomeVisible { get; set; } = new()
    {
        ["cora"] = true,
        ["balance"] = true,
        ["quickActions"] = true,
        ["performance"] = true,
        [SectionHoldings] = true,
        [SectionLocalWallets] = true,
    };

    /// <summary>separate (default) = two tables; combined = one table with green/gold row accents.</summary>
    public string AssetsLayout { get; set; } = "separate";

    public bool ValuesHiddenOnLaunch { get; set; }

    public bool CoraEnabled { get; set; } = true;

    public string DefaultSendSpeed { get; set; } = "instant";

    public string Appearance { get; set; } = "dark";

    public string BaseCurrency { get; set; } = "USD";

    /// <summary>Symbols visible on Home selectors / charts (uppercase tickers).</summary>
    public Collection<string> EnabledCurrencies { get; set; } = new(DefaultEnabledCurrencies.ToList());

    public int LockIdleSeconds { get; set; } = 120;

    /// <summary>
    /// Replaces <see cref="HomeOrder"/> contents (JSON/wire apply and tests).
    /// Use: Medium (prefs sync). Scope: this prefs model.
    /// </summary>
    public void ReplaceHomeOrder(IEnumerable<string> order)
    {
        HomeOrder.Clear();
        foreach (var item in order)
        {
            HomeOrder.Add(item);
        }
    }

    /// <summary>
    /// Replaces <see cref="EnabledCurrencies"/> contents (JSON/wire apply and profile save).
    /// Use: Medium (prefs sync). Scope: this prefs model.
    /// </summary>
    public void ReplaceEnabledCurrencies(IEnumerable<string> currencies)
    {
        EnabledCurrencies.Clear();
        foreach (var item in currencies)
        {
            EnabledCurrencies.Add(item);
        }
    }

    /// <summary>
    /// Replaces <see cref="HomeVisible"/> contents (JSON/wire apply and tests).
    /// Use: Medium (prefs sync). Scope: this prefs model.
    /// </summary>
    public void ReplaceHomeVisible(IEnumerable<KeyValuePair<string, bool>> visible)
    {
        HomeVisible.Clear();
        foreach (KeyValuePair<string, bool> item in visible)
        {
            HomeVisible[item.Key] = item.Value;
        }
    }

    /// <summary>Migrate legacy Expo-style <c>assets</c> key and ensure holdings/local keys exist.</summary>
    public void NormalizeHomeSections()
    {
        MigrateLegacyAssetsSection();
        EnsureHomeSectionKeys();
        NormalizeAssetsLayout();
        NormalizeEnabledCurrencies();
        NormalizeDefaultSendSpeed();
    }

    private void MigrateLegacyAssetsSection()
    {
        if (!HomeOrder.Contains(SectionLegacyAssets))
        {
            return;
        }

        var idx = HomeOrder.IndexOf(SectionLegacyAssets);
        HomeOrder.RemoveAt(idx);
        if (!HomeOrder.Contains(SectionHoldings))
        {
            HomeOrder.Insert(idx, SectionHoldings);
            idx++;
        }

        if (!HomeOrder.Contains(SectionLocalWallets))
        {
            HomeOrder.Insert(idx, SectionLocalWallets);
        }
    }

    private void EnsureHomeSectionKeys()
    {
        foreach (var key in DefaultHomeOrder)
        {
            if (!HomeOrder.Contains(key))
            {
                HomeOrder.Add(key);
            }

            if (!HomeVisible.ContainsKey(key))
            {
                HomeVisible[key] = ResolveHomeVisibleDefault(key);
            }
        }

        HomeVisible.Remove(SectionLegacyAssets);
    }

    /// <summary>
    /// Defaults a missing home-section visibility flag, migrating legacy "assets" into holdings/localWallets.
    /// Use: Medium (NormalizeHomeSections). Scope: this prefs model.
    /// </summary>
    private bool ResolveHomeVisibleDefault(string key)
    {
        if (key != SectionHoldings && key != SectionLocalWallets)
        {
            return true;
        }

        if (!HomeVisible.ContainsKey(SectionLegacyAssets))
        {
            return true;
        }

        return HomeVisible.TryGetValue(SectionLegacyAssets, out var assetsVisible) && assetsVisible;
    }

    private void NormalizeAssetsLayout()
    {
        if (string.IsNullOrWhiteSpace(AssetsLayout)
            || (AssetsLayout is not "separate" and not "combined"))
        {
            AssetsLayout = "separate";
        }
    }

    private void NormalizeEnabledCurrencies()
    {
        if (EnabledCurrencies.Count == 0)
        {
            ReplaceEnabledCurrencies(DefaultEnabledCurrencies);
            return;
        }

        var normalized = EnabledCurrencies
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        ReplaceEnabledCurrencies(normalized.Count == 0 ? DefaultEnabledCurrencies : normalized);
    }

    private void NormalizeDefaultSendSpeed()
    {
        if (string.IsNullOrWhiteSpace(DefaultSendSpeed)
            || (DefaultSendSpeed is not "instant" and not "ach"))
        {
            DefaultSendSpeed = "instant";
        }
    }
}
