using CipherBank_app.Views;

namespace CipherBank_app;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
        Routing.RegisterRoute(nameof(WalletPage), typeof(WalletPage));
        Routing.RegisterRoute(nameof(PurchasePage), typeof(PurchasePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}