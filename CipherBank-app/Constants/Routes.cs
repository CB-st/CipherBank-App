// <copyright file="Routes.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Constants;

/// <summary>Shell navigation route constants (Cora IA).</summary>
public static class Routes
{
    public const string Splash = "//SplashPage";
    public const string Welcome = "//WelcomePage";
    public const string Keys = "//KeysPage";
    public const string BackupQuiz = "//BackupQuizPage";
    public const string SetPin = "//SetPinPage";
    public const string Unlock = "//UnlockPage";
    public const string Home = "//HomePage";
    public const string Convert = "//ConvertPage";
    public const string Pay = "//PayPage";
    public const string Send = "//SendPage";
    public const string Receive = "//ReceivePage";
    public const string Profile = "//ProfilePage";
    public const string PosLab = "PosLabPage";
    public const string AddWallet = "AddWalletPage";
    public const string RestoreBackup = "RestoreBackupPage";
    public const string ChangePin = "ChangePinPage";

    // Legacy (parked)
    public const string Login = "//LoginPage";
    public const string Dashboard = "//DashboardPage";
    public const string Wallet = "//WalletPage";
    public const string Purchase = "//PurchasePage";
    public const string Settings = "//SettingsPage";

    public static string PurchaseWithSymbol(string symbol)
        => $"{Purchase}?symbol={Uri.EscapeDataString(symbol)}";
}
