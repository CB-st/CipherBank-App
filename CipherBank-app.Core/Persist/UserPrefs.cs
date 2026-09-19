// <copyright file="UserPrefs.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Mutable per-user device preferences initialized from configured defaults.</summary>
public sealed class UserPrefs
{
    private readonly UserPreferenceDefaults _defaults;

    public UserPrefs(UserPreferenceDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        _defaults = defaults;
        HomeOrder = new Collection<string>(defaults.HomeOrder.ToList());
        HomeVisible = new Dictionary<string, bool>(defaults.HomeVisible, StringComparer.Ordinal);
        EnabledCurrencies = new Collection<string>(
            defaults.EnabledCurrencies.Select(symbol => symbol.Value).ToList());
        AssetsLayout = defaults.AssetsLayout;
        ValuesHiddenOnLaunch = defaults.ValuesHiddenOnLaunch;
        CoraEnabled = defaults.CoraEnabled;
        DefaultSendSpeed = defaults.DefaultSendSpeed;
        Appearance = defaults.Appearance;
        BaseCurrency = defaults.BaseCurrency.Value;
        LockIdleSeconds = defaults.LockIdleSeconds;
    }

    public static string SectionHoldings { get; } = "holdings";

    public static string SectionLocalWallets { get; } = "localWallets";

    public static string SectionLegacyAssets { get; } = "assets";

    public Collection<string> HomeOrder { get; private set; }

    public Dictionary<string, bool> HomeVisible { get; private set; }

    /// <summary>separate (default) = two tables; combined = one table with green/gold row accents.</summary>
    public string AssetsLayout { get; set; }

    public bool ValuesHiddenOnLaunch { get; set; }

    public bool CoraEnabled { get; set; }

    public string DefaultSendSpeed { get; set; }

    public string Appearance { get; set; }

    public string BaseCurrency { get; set; }

    /// <summary>Symbols visible on Home selectors / charts (uppercase tickers).</summary>
    public Collection<string> EnabledCurrencies { get; private set; }

    public int LockIdleSeconds { get; set; }

    /// <summary>
    /// Replaces <see cref="HomeOrder"/> contents (JSON/wire apply and tests).
    /// Use: Medium (prefs sync). Scope: this prefs model.
    /// </summary>
    public void ReplaceHomeOrder(IEnumerable<string> order)
    {
        HomeOrder.Clear();
        foreach (string item in order)
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
        foreach (string item in currencies)
        {
            EnabledCurrencies.Add(item);
        }
    }

    /// <summary>
    /// Replaces <see cref="HomeVisible"/> contents (JSON/wire apply and tests).
    /// Use: Medium (prefs sync). Scope: this prefs model.
    /// </summary>
    public void ReplaceHomeVisible(IReadOnlyDictionary<string, bool> visible)
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

        int idx = HomeOrder.IndexOf(SectionLegacyAssets);
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
        foreach (string key in _defaults.HomeOrder)
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
            return _defaults.HomeVisible.GetValueOrDefault(key, true);
        }

        if (!HomeVisible.ContainsKey(SectionLegacyAssets))
        {
            return true;
        }

        return HomeVisible.TryGetValue(SectionLegacyAssets, out bool assetsVisible) && assetsVisible;
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
            ReplaceEnabledCurrencies(_defaults.EnabledCurrencies.Select(symbol => symbol.Value));
            return;
        }

        List<string> normalized = EnabledCurrencies
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => AssetSymbol.Parse(s).Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        ReplaceEnabledCurrencies(
            normalized.Count == 0
                ? _defaults.EnabledCurrencies.Select(symbol => symbol.Value)
                : normalized);
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
