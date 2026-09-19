// <copyright file="TestPreferenceDefaults.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Tests.Persist;

internal static class TestPreferenceDefaults
{
    internal static UserPreferenceDefaults Value { get; } =
        UserPreferenceDefaults.FromOptions(CreateOptions());

    internal static IOptions<UserPreferenceDefaultsOptions> Options { get; } =
        Microsoft.Extensions.Options.Options.Create(CreateOptions());

    private static UserPreferenceDefaultsOptions CreateOptions()
    {
        UserPreferenceDefaultsOptions options = new()
        {
            AssetsLayout = "separate",
            CoraEnabled = true,
            DefaultSendSpeed = "instant",
            Appearance = "dark",
            BaseCurrency = "USD",
            LockIdleSeconds = 120,
        };
        foreach (string key in new[]
                 {
                     "cora", "balance", "quickActions", "performance", "holdings", "localWallets",
                 })
        {
            options.HomeOrder.Add(key);
            options.HomeVisible[key] = true;
        }

        foreach (string symbol in new[] { "BTC", "XMR", "USD" })
        {
            options.EnabledCurrencies.Add(symbol);
        }

        return options;
    }
}
