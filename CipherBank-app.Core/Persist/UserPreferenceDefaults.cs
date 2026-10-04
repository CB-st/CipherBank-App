// <copyright file="UserPreferenceDefaults.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using CipherBank_app.Configuration;
using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Immutable validated defaults used only for missing preference values.</summary>
public sealed record UserPreferenceDefaults(
    ImmutableArray<string> HomeOrder,
    ImmutableDictionary<string, bool> HomeVisible,
    ImmutableArray<AssetSymbol> EnabledCurrencies,
    string AssetsLayout,
    bool ValuesHiddenOnLaunch,
    bool CoraEnabled,
    string DefaultSendSpeed,
    string Appearance,
    AssetSymbol BaseCurrency,
    int LockIdleSeconds)
{
    public static UserPreferenceDefaults FromOptions(UserPreferenceDefaultsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
        {
            throw new ArgumentException("User preference defaults are invalid.", nameof(options));
        }

        return new UserPreferenceDefaults(
            [.. options.HomeOrder],
            options.HomeVisible.ToImmutableDictionary(StringComparer.Ordinal),
            [.. options.EnabledCurrencies.Select(AssetSymbol.Parse)],
            options.AssetsLayout,
            options.ValuesHiddenOnLaunch,
            options.CoraEnabled,
            options.DefaultSendSpeed,
            options.Appearance,
            AssetSymbol.Parse(options.BaseCurrency),
            options.LockIdleSeconds);
    }
}
