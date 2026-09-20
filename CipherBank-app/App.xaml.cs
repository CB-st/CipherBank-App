// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using Serilog;

namespace CipherBank_app;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    public App(IRecipientSeedInitializer recipientSeeds)
    {
        InitializeComponent();

        // MAUI has no async build hook (IMauiInitializeService is synchronous), so the
        // App constructor is the defined async startup path: start the initialization
        // task after the provider is built and surface failures through the log.
        _ = SeedRecipientsAsync(recipientSeeds);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    /// <summary>
    /// Seeds configured default recipients into a new database at startup.
    /// Failures are logged and never fatal; seeding is idempotent per configured ID.
    /// Use: Low (once per cold start). Scope: app startup.
    /// </summary>
    private static async Task SeedRecipientsAsync(IRecipientSeedInitializer recipientSeeds)
    {
        try
        {
            await recipientSeeds.InitializeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Recipient seed initialization failed");
        }
    }
}
