// <copyright file="PrefsWire.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;
using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <summary>Wire DTO for GET/PUT /v1/prefs (SCREAMING_SNAKE; camelCase accepted on read).</summary>
public sealed class PrefsWireDto
{
    [JsonPropertyName("HOME_ORDER")]
    public List<string>? HomeOrder { get; set; }

    [JsonPropertyName("homeOrder")]
    public List<string>? HomeOrderCamel { get; set; }

    [JsonPropertyName("HOME_VISIBLE")]
    public Dictionary<string, bool>? HomeVisible { get; set; }

    [JsonPropertyName("homeVisible")]
    public Dictionary<string, bool>? HomeVisibleCamel { get; set; }

    [JsonPropertyName("ASSETS_LAYOUT")]
    public string? AssetsLayout { get; set; }

    [JsonPropertyName("assetsLayout")]
    public string? AssetsLayoutCamel { get; set; }

    [JsonPropertyName("VALUES_HIDDEN_ON_LAUNCH")]
    public bool? ValuesHiddenOnLaunch { get; set; }

    [JsonPropertyName("valuesHiddenOnLaunch")]
    public bool? ValuesHiddenOnLaunchCamel { get; set; }

    [JsonPropertyName("CORA_ENABLED")]
    public bool? CoraEnabled { get; set; }

    [JsonPropertyName("coraEnabled")]
    public bool? CoraEnabledCamel { get; set; }

    [JsonPropertyName("DEFAULT_SEND_SPEED")]
    public string? DefaultSendSpeed { get; set; }

    [JsonPropertyName("defaultSendSpeed")]
    public string? DefaultSendSpeedCamel { get; set; }

    [JsonPropertyName("APPEARANCE")]
    public string? Appearance { get; set; }

    [JsonPropertyName("appearance")]
    public string? AppearanceCamel { get; set; }

    [JsonPropertyName("BASE_CURRENCY")]
    public string? BaseCurrency { get; set; }

    [JsonPropertyName("baseCurrency")]
    public string? BaseCurrencyCamel { get; set; }

    [JsonPropertyName("ENABLED_CURRENCIES")]
    public List<string>? EnabledCurrencies { get; set; }

    [JsonPropertyName("enabledCurrencies")]
    public List<string>? EnabledCurrenciesCamel { get; set; }

    [JsonPropertyName("LOCK_IDLE_SECONDS")]
    public int? LockIdleSeconds { get; set; }

    [JsonPropertyName("appLockIdleSec")]
    public int? AppLockIdleSecCamel { get; set; }

    public static PrefsWireDto FromUserPrefs(UserPrefs prefs)
        => new()
        {
            HomeOrder = new List<string>(prefs.HomeOrder),
            HomeVisible = new Dictionary<string, bool>(prefs.HomeVisible),
            AssetsLayout = prefs.AssetsLayout,
            ValuesHiddenOnLaunch = prefs.ValuesHiddenOnLaunch,
            CoraEnabled = prefs.CoraEnabled,
            DefaultSendSpeed = prefs.DefaultSendSpeed,
            Appearance = prefs.Appearance,
            BaseCurrency = prefs.BaseCurrency,
            EnabledCurrencies = new List<string>(prefs.EnabledCurrencies),
            LockIdleSeconds = prefs.LockIdleSeconds,
        };

    public void ApplyOnto(UserPrefs target)
    {
        ApplyHomeSectionsOnto(target);
        ApplyDisplayPrefsOnto(target);
        ApplyCurrencyPrefsOnto(target);
        ApplyLockIdleOnto(target);
        target.NormalizeHomeSections();
    }

    private void ApplyHomeSectionsOnto(UserPrefs target)
    {
        List<string>? order = HomeOrder ?? HomeOrderCamel;
        if (order is { Count: > 0 })
        {
            target.HomeOrder = new List<string>(order);
        }

        Dictionary<string, bool>? visible = HomeVisible ?? HomeVisibleCamel;
        if (visible is not null)
        {
            foreach (KeyValuePair<string, bool> kv in visible)
            {
                target.HomeVisible[kv.Key] = kv.Value;
            }
        }

        string? layout = AssetsLayout ?? AssetsLayoutCamel;
        if (!string.IsNullOrWhiteSpace(layout))
        {
            target.AssetsLayout = layout;
        }
    }

    private void ApplyDisplayPrefsOnto(UserPrefs target)
    {
        bool? hide = ValuesHiddenOnLaunch ?? ValuesHiddenOnLaunchCamel;
        if (hide is bool hideValue)
        {
            target.ValuesHiddenOnLaunch = hideValue;
        }

        bool? cora = CoraEnabled ?? CoraEnabledCamel;
        if (cora is bool coraValue)
        {
            target.CoraEnabled = coraValue;
        }

        string? speed = DefaultSendSpeed ?? DefaultSendSpeedCamel;
        if (!string.IsNullOrWhiteSpace(speed))
        {
            target.DefaultSendSpeed = speed;
        }

        string? appearance = Appearance ?? AppearanceCamel;
        if (!string.IsNullOrWhiteSpace(appearance))
        {
            target.Appearance = appearance;
        }
    }

    private void ApplyCurrencyPrefsOnto(UserPrefs target)
    {
        string? currency = BaseCurrency ?? BaseCurrencyCamel;
        if (!string.IsNullOrWhiteSpace(currency))
        {
            target.BaseCurrency = currency;
        }

        List<string>? enabled = EnabledCurrencies ?? EnabledCurrenciesCamel;
        if (enabled is { Count: > 0 })
        {
            target.EnabledCurrencies = new List<string>(enabled);
        }
    }

    private void ApplyLockIdleOnto(UserPrefs target)
    {
        int? idle = LockIdleSeconds ?? AppLockIdleSecCamel;
        if (idle is int seconds && seconds > 0)
        {
            target.LockIdleSeconds = seconds;
        }
    }
}

/// <summary>Merge remote prefs into local. Local keeps AssetsLayout when remote omits it.</summary>
public static class PrefsMerge
{
    public static UserPrefs Merge(UserPrefs local, PrefsWireDto? remote)
    {
        ArgumentNullException.ThrowIfNull(local);
        if (remote is null)
        {
            local.NormalizeHomeSections();
            return local;
        }

        string priorLayout = local.AssetsLayout;
        bool remoteHadLayout = !string.IsNullOrWhiteSpace(remote.AssetsLayout ?? remote.AssetsLayoutCamel);
        remote.ApplyOnto(local);
        if (!remoteHadLayout)
        {
            local.AssetsLayout = priorLayout;
        }

        local.NormalizeHomeSections();
        return local;
    }
}
