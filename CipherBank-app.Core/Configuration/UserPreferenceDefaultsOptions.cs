// <copyright file="UserPreferenceDefaultsOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Configurable first-run defaults for mutable user preferences.</summary>
public sealed class UserPreferenceDefaultsOptions
{
    /// <summary>Gets the ordered Home section keys used when a preference row is missing.</summary>
    public IList<string> HomeOrder { get; } = [];

    /// <summary>Gets per-section Home visibility used when a preference row is missing.</summary>
    public IDictionary<string, bool> HomeVisible { get; } = new Dictionary<string, bool>();

    /// <summary>Gets the asset tickers enabled when a preference row is missing.</summary>
    public IList<string> EnabledCurrencies { get; } = [];

    /// <summary>Gets or sets the assets layout, <c>separate</c> or <c>combined</c>.</summary>
    public string AssetsLayout { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether values start hidden.</summary>
    public bool ValuesHiddenOnLaunch { get; set; }

    /// <summary>Gets or sets a value indicating whether Cora starts enabled.</summary>
    public bool CoraEnabled { get; set; }

    /// <summary>Gets or sets the default send speed, <c>instant</c> or <c>ach</c>.</summary>
    public string DefaultSendSpeed { get; set; } = string.Empty;

    /// <summary>Gets or sets the appearance mode: <c>system</c>, <c>light</c>, or <c>dark</c>.</summary>
    public string Appearance { get; set; } = string.Empty;

    /// <summary>Gets or sets the base currency ticker.</summary>
    public string BaseCurrency { get; set; } = string.Empty;

    /// <summary>Gets or sets the idle seconds before lock. Zero uses the host default.</summary>
    public int LockIdleSeconds { get; set; }

    /// <summary>
    /// Returns whether the bound defaults are safe to fill a missing preference row.
    /// Use: Low (startup validation). Scope: UserPreferenceDefaultsOptions.
    /// </summary>
    /// <returns><see langword="true"/> when layout, speed, appearance, and lists are usable.</returns>
    public bool IsValid() => this is
    {
        AssetsLayout: "separate" or "combined",
        DefaultSendSpeed: "instant" or "ach",
        Appearance: "system" or "light" or "dark",
        LockIdleSeconds: >= 0,
    }

    && HasNonblankListsAndBaseCurrency();

    private static bool HasNonblankValues(ICollection<string> values) =>
        values.Count != 0 && !values.Any(static value => string.IsNullOrWhiteSpace(value));

    private bool HasNonblankListsAndBaseCurrency() =>
        HasNonblankValues(HomeOrder)
        && HasNonblankValues(EnabledCurrencies)
        && !string.IsNullOrWhiteSpace(BaseCurrency);
}
