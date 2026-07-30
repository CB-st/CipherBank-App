// <copyright file="PrefsWireDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <summary>
/// Wire DTO for GET/PUT /v1/prefs (SCREAMING_SNAKE on write; camelCase accepted on read via ExtensionData).
/// </summary>
public sealed class PrefsWireDto
{
    [JsonPropertyName("HOME_ORDER")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<string> HomeOrder { get; } = [];

    [JsonPropertyName("HOME_VISIBLE")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, bool> HomeVisible { get; } = new();

    [JsonPropertyName("ASSETS_LAYOUT")]
    public string? AssetsLayout { get; set; }

    [JsonPropertyName("VALUES_HIDDEN_ON_LAUNCH")]
    public bool? ValuesHiddenOnLaunch { get; set; }

    [JsonPropertyName("CORA_ENABLED")]
    public bool? CoraEnabled { get; set; }

    [JsonPropertyName("DEFAULT_SEND_SPEED")]
    public string? DefaultSendSpeed { get; set; }

    [JsonPropertyName("APPEARANCE")]
    public string? Appearance { get; set; }

    [JsonPropertyName("BASE_CURRENCY")]
    public string? BaseCurrency { get; set; }

    [JsonPropertyName("ENABLED_CURRENCIES")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<string> EnabledCurrencies { get; } = [];

    [JsonPropertyName("LOCK_IDLE_SECONDS")]
    public int? LockIdleSeconds { get; set; }

    /// <summary>Captures camelCase / alt keys (e.g. appLockIdleSec) for fold-in after deserialize.</summary>
    [JsonExtensionData]
    [JsonInclude]
    public Dictionary<string, JsonElement>? ExtensionData { get; private set; }

    public static PrefsWireDto FromUserPrefs(UserPrefs prefs)
    {
        PrefsWireDto dto = new()
        {
            AssetsLayout = prefs.AssetsLayout,
            ValuesHiddenOnLaunch = prefs.ValuesHiddenOnLaunch,
            CoraEnabled = prefs.CoraEnabled,
            DefaultSendSpeed = prefs.DefaultSendSpeed,
            Appearance = prefs.Appearance,
            BaseCurrency = prefs.BaseCurrency,
            LockIdleSeconds = prefs.LockIdleSeconds,
        };
        dto.ReplaceHomeOrder(prefs.HomeOrder);
        dto.ReplaceHomeVisible(prefs.HomeVisible);
        dto.ReplaceEnabledCurrencies(prefs.EnabledCurrencies);
        return dto;
    }

    public void ReplaceHomeOrder(IEnumerable<string> order)
    {
        HomeOrder.Clear();
        foreach (string item in order)
        {
            HomeOrder.Add(item);
        }
    }

    public void ReplaceHomeVisible(IEnumerable<KeyValuePair<string, bool>> visible)
    {
        HomeVisible.Clear();
        foreach (KeyValuePair<string, bool> item in visible)
        {
            HomeVisible[item.Key] = item.Value;
        }
    }

    public void ReplaceEnabledCurrencies(IEnumerable<string> currencies)
    {
        EnabledCurrencies.Clear();
        foreach (string item in currencies)
        {
            EnabledCurrencies.Add(item);
        }
    }

    /// <summary>
    /// Folds camelCase extension keys into primary SCREAMING_SNAKE properties once.
    /// Use: High (deserialize / ApplyOnto). Scope: this DTO.
    /// </summary>
    public void FoldAlternateNames()
    {
        Dictionary<string, JsonElement>? data = ExtensionData;
        if (data is null || data.Count == 0)
        {
            return;
        }

        FoldHomeFields(data);
        FoldDisplayFields(data);
        FoldCurrencyAndLockFields(data);
        ExtensionData = null;
    }

    /// <summary>
    /// Applies folded wire prefs onto a local <see cref="UserPrefs"/> model.
    /// Use: High (prefs sync). Scope: this DTO.
    /// </summary>
    public void ApplyOnto(UserPrefs target)
    {
        FoldAlternateNames();
        ApplyHomeSectionsOnto(target);
        ApplyDisplayPrefsOnto(target);
        ApplyCurrencyPrefsOnto(target);
        ApplyLockIdleOnto(target);
        target.NormalizeHomeSections();
    }

    private void FoldHomeFields(IDictionary<string, JsonElement> data)
    {
        if (HomeOrder.Count == 0 && WireJson.TryGetStringList(data, "homeOrder") is { } homeOrder)
        {
            ReplaceHomeOrder(homeOrder);
        }

        if (HomeVisible.Count == 0 && WireJson.TryGetBoolMap(data, "homeVisible") is { } homeVisible)
        {
            ReplaceHomeVisible(homeVisible);
        }

        AssetsLayout ??= WireJson.TryGetString(data, "assetsLayout");
    }

    private void FoldDisplayFields(IDictionary<string, JsonElement> data)
    {
        ValuesHiddenOnLaunch ??= WireJson.TryGetBool(data, "valuesHiddenOnLaunch");
        CoraEnabled ??= WireJson.TryGetBool(data, "coraEnabled");
        DefaultSendSpeed ??= WireJson.TryGetString(data, "defaultSendSpeed");
        Appearance ??= WireJson.TryGetString(data, "appearance");
    }

    private void FoldCurrencyAndLockFields(IDictionary<string, JsonElement> data)
    {
        BaseCurrency ??= WireJson.TryGetString(data, "baseCurrency");
        if (EnabledCurrencies.Count == 0 && WireJson.TryGetStringList(data, "enabledCurrencies") is { } enabled)
        {
            ReplaceEnabledCurrencies(enabled);
        }

        LockIdleSeconds ??= ResolveLockIdleSeconds(data);

        static int? ResolveLockIdleSeconds(IDictionary<string, JsonElement> source)
        {
            if (WireJson.TryGetInt64(source, "appLockIdleSec") is long idle)
            {
                return (int)idle;
            }

            if (WireJson.TryGetInt64(source, "lockIdleSeconds") is long lockIdle)
            {
                return (int)lockIdle;
            }

            return null;
        }
    }

    private void ApplyHomeSectionsOnto(UserPrefs target)
    {
        if (HomeOrder is { Count: > 0 })
        {
            target.ReplaceHomeOrder(HomeOrder);
        }

        if (HomeVisible.Count > 0)
        {
            foreach (KeyValuePair<string, bool> kv in HomeVisible)
            {
                target.HomeVisible[kv.Key] = kv.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(AssetsLayout))
        {
            target.AssetsLayout = AssetsLayout;
        }
    }

    private void ApplyDisplayPrefsOnto(UserPrefs target)
    {
        if (ValuesHiddenOnLaunch is bool hideValue)
        {
            target.ValuesHiddenOnLaunch = hideValue;
        }

        if (CoraEnabled is bool coraValue)
        {
            target.CoraEnabled = coraValue;
        }

        if (!string.IsNullOrWhiteSpace(DefaultSendSpeed))
        {
            target.DefaultSendSpeed = DefaultSendSpeed;
        }

        if (!string.IsNullOrWhiteSpace(Appearance))
        {
            target.Appearance = Appearance;
        }
    }

    private void ApplyCurrencyPrefsOnto(UserPrefs target)
    {
        if (!string.IsNullOrWhiteSpace(BaseCurrency))
        {
            target.BaseCurrency = BaseCurrency;
        }

        if (EnabledCurrencies is { Count: > 0 })
        {
            target.ReplaceEnabledCurrencies(EnabledCurrencies);
        }
    }

    private void ApplyLockIdleOnto(UserPrefs target)
    {
        if (LockIdleSeconds is int seconds && seconds > 0)
        {
            target.LockIdleSeconds = seconds;
        }
    }
}
