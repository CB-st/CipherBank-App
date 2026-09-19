// <copyright file="UserPrefsWireDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Nullable local JSON wire shape used to merge configured defaults safely.</summary>
public sealed class UserPrefsWireDto
{
    public List<string>? HomeOrder { get; set; }

    public Dictionary<string, bool>? HomeVisible { get; set; }

    public string? AssetsLayout { get; set; }

    public bool? ValuesHiddenOnLaunch { get; set; }

    public bool? CoraEnabled { get; set; }

    public string? DefaultSendSpeed { get; set; }

    public string? Appearance { get; set; }

    public string? BaseCurrency { get; set; }

    public List<string>? EnabledCurrencies { get; set; }

    public int? LockIdleSeconds { get; set; }

    internal static UserPrefsWireDto FromPrefs(UserPrefs prefs) =>
        new()
        {
            HomeOrder = [.. prefs.HomeOrder],
            HomeVisible = new Dictionary<string, bool>(prefs.HomeVisible, StringComparer.Ordinal),
            AssetsLayout = prefs.AssetsLayout,
            ValuesHiddenOnLaunch = prefs.ValuesHiddenOnLaunch,
            CoraEnabled = prefs.CoraEnabled,
            DefaultSendSpeed = prefs.DefaultSendSpeed,
            Appearance = prefs.Appearance,
            BaseCurrency = prefs.BaseCurrency,
            EnabledCurrencies = [.. prefs.EnabledCurrencies],
            LockIdleSeconds = prefs.LockIdleSeconds,
        };

    internal UserPrefs ToPrefs(UserPreferenceDefaults defaults)
    {
        UserPrefs prefs = new(defaults);
        ApplyScalarValues(prefs);
        ApplyCollectionValues(prefs);
        prefs.NormalizeHomeSections();
        return prefs;
    }

    private void ApplyCollectionValues(UserPrefs prefs)
    {
        if (HomeOrder is not null)
        {
            prefs.ReplaceHomeOrder(HomeOrder);
        }

        if (HomeVisible is not null)
        {
            prefs.ReplaceHomeVisible(HomeVisible);
        }

        if (EnabledCurrencies is not null)
        {
            prefs.ReplaceEnabledCurrencies(EnabledCurrencies);
        }
    }

    private void ApplyScalarValues(UserPrefs prefs)
    {
        prefs.AssetsLayout = AssetsLayout ?? prefs.AssetsLayout;
        prefs.ValuesHiddenOnLaunch = ValuesHiddenOnLaunch ?? prefs.ValuesHiddenOnLaunch;
        prefs.CoraEnabled = CoraEnabled ?? prefs.CoraEnabled;
        prefs.DefaultSendSpeed = DefaultSendSpeed ?? prefs.DefaultSendSpeed;
        prefs.Appearance = Appearance ?? prefs.Appearance;
        prefs.BaseCurrency = BaseCurrency ?? prefs.BaseCurrency;
        prefs.LockIdleSeconds = LockIdleSeconds ?? prefs.LockIdleSeconds;
    }
}
