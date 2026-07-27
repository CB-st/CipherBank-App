// <copyright file="UserPrefs.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>User preference model (Cora prefs).</summary>
public sealed class UserPrefs
{
    public static readonly string[] DefaultHomeOrder =
    {
        "cora", "balance", "quickActions", "performance", "holdings", "localWallets",
    };

    public static readonly string[] DefaultEnabledCurrencies = { "BTC", "XMR", "USD" };

    public List<string> HomeOrder { get; set; } = new(DefaultHomeOrder);

    public Dictionary<string, bool> HomeVisible { get; set; } = new()
    {
        ["cora"] = true,
        ["balance"] = true,
        ["quickActions"] = true,
        ["performance"] = true,
        ["holdings"] = true,
        ["localWallets"] = true,
    };

    /// <summary>separate (default) = two tables; combined = one table with green/gold row accents.</summary>
    public string AssetsLayout { get; set; } = "separate";

    public bool ValuesHiddenOnLaunch { get; set; }

    public bool CoraEnabled { get; set; } = true;

    public string DefaultSendSpeed { get; set; } = "instant";

    public string Appearance { get; set; } = "dark";

    public string BaseCurrency { get; set; } = "USD";

    /// <summary>Symbols visible on Home selectors / charts (uppercase tickers).</summary>
    public List<string> EnabledCurrencies { get; set; } = new(DefaultEnabledCurrencies);

    public int LockIdleSeconds { get; set; } = 120;

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
        if (!HomeOrder.Contains("assets"))
        {
            return;
        }

        int idx = HomeOrder.IndexOf("assets");
        HomeOrder.RemoveAt(idx);
        if (!HomeOrder.Contains("holdings"))
        {
            HomeOrder.Insert(idx, "holdings");
            idx++;
        }

        if (!HomeOrder.Contains("localWallets"))
        {
            HomeOrder.Insert(idx, "localWallets");
        }
    }

    private void EnsureHomeSectionKeys()
    {
        foreach (string key in DefaultHomeOrder)
        {
            if (!HomeOrder.Contains(key))
            {
                HomeOrder.Add(key);
            }

            if (!HomeVisible.ContainsKey(key))
            {
                bool legacyAssets = HomeVisible.TryGetValue("assets", out bool assetsVisible) && assetsVisible;
                HomeVisible[key] = key is "holdings" or "localWallets"
                    ? (HomeVisible.ContainsKey("assets") ? legacyAssets : true)
                    : true;
            }
        }

        HomeVisible.Remove("assets");
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
        if (EnabledCurrencies is null || EnabledCurrencies.Count == 0)
        {
            EnabledCurrencies = new List<string>(DefaultEnabledCurrencies);
            return;
        }

        EnabledCurrencies = EnabledCurrencies
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (EnabledCurrencies.Count == 0)
        {
            EnabledCurrencies = new List<string>(DefaultEnabledCurrencies);
        }
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
