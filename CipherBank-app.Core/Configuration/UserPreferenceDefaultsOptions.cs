// <copyright file="UserPreferenceDefaultsOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Configurable first-run defaults for mutable user preferences.</summary>
public sealed class UserPreferenceDefaultsOptions
{
    public IList<string> HomeOrder { get; } = [];

    public IDictionary<string, bool> HomeVisible { get; } = new Dictionary<string, bool>();

    public IList<string> EnabledCurrencies { get; } = [];

    public string AssetsLayout { get; set; } = string.Empty;

    public bool ValuesHiddenOnLaunch { get; set; }

    public bool CoraEnabled { get; set; }

    public string DefaultSendSpeed { get; set; } = string.Empty;

    public string Appearance { get; set; } = string.Empty;

    public string BaseCurrency { get; set; } = string.Empty;

    public int LockIdleSeconds { get; set; }

    public bool IsValid() =>
        HomeOrder.Count > 0
        && HomeOrder.All(static value => !string.IsNullOrWhiteSpace(value))
        && EnabledCurrencies.Count > 0
        && EnabledCurrencies.All(static value => !string.IsNullOrWhiteSpace(value))
        && AssetsLayout is "separate" or "combined"
        && DefaultSendSpeed is "instant" or "ach"
        && Appearance is "system" or "light" or "dark"
        && !string.IsNullOrWhiteSpace(BaseCurrency)
        && LockIdleSeconds >= 0;
}
