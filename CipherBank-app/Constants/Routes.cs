// <copyright file="Routes.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Constants;

/// <summary>
/// Shell navigation route constants.
/// </summary>
public static class Routes
{
    public const string Login = "//LoginPage";
    public const string Dashboard = "//DashboardPage";
    public const string Wallet = "//WalletPage";
    public const string Purchase = "//PurchasePage";
    public const string Settings = "//SettingsPage";

    public static string PurchaseWithSymbol(AssetSymbol symbol) =>
        $"//PurchasePage?symbol={Uri.EscapeDataString(symbol.Value)}";
}
