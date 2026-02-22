// <copyright file="Routes.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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

    public static string PurchaseWithSymbol(string symbol) => $"//PurchasePage?symbol={symbol}";
}
