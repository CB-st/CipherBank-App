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

    public bool IsValid() => this switch
    {
        {
            AssetsLayout: "separate" or "combined",
            DefaultSendSpeed: "instant" or "ach",
            Appearance: "system" or "light" or "dark",
            LockIdleSeconds: >= 0,
        }

        when HasNonblankListsAndBaseCurrency() => true,
        _ => false,
    };

    private static bool HasNonblankValues(ICollection<string> values) =>
        values.Count > 0 && values.All(static value => !string.IsNullOrWhiteSpace(value));

    private bool HasNonblankListsAndBaseCurrency() =>
        HasNonblankValues(HomeOrder)
        && HasNonblankValues(EnabledCurrencies)
        && !string.IsNullOrWhiteSpace(BaseCurrency);
}
