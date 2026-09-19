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
        HasNonblankValues(HomeOrder)
        && HasNonblankValues(EnabledCurrencies)
        && IsLayoutValid()
        && IsSendSpeedValid()
        && IsAppearanceValid()
        && !string.IsNullOrWhiteSpace(BaseCurrency)
        && LockIdleSeconds >= 0;

    private static bool HasNonblankValues(ICollection<string> values) =>
        values.Count > 0 && values.All(static value => !string.IsNullOrWhiteSpace(value));

    private bool IsLayoutValid() => AssetsLayout is "separate" or "combined";

    private bool IsSendSpeedValid() => DefaultSendSpeed is "instant" or "ach";

    private bool IsAppearanceValid() => Appearance is "system" or "light" or "dark";
}
