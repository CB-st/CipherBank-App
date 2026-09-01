// <copyright file="ProfileViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using CipherBank_app.Constants;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Profile / prefs / vault — Phase D polished.</summary>
public partial class ProfileViewModel : ObservableObject, IDisposable
{
    private static readonly Dictionary<string, string> SectionLabels = new()
    {
        ["cora"] = "Cora bar",
        ["balance"] = "Balance hero",
        ["quickActions"] = "Quick actions",
        ["performance"] = "Performance",
        ["holdings"] = "Holdings",
        ["localWallets"] = "Local wallets",
        ["assets"] = "Assets (legacy)",
    };

    private static readonly string[] AppearanceChoices = ["dark", "light"];
    private static readonly string[] BaseCurrencyChoices = ["USD", "BTC", "EUR", "JPY"];
    private static readonly string[] SendSpeedChoices = ["instant", "ach"];
    private static readonly string[] CurrencyCatalog = ["BTC", "XMR", "USD", "ETH"];

    // --- Mnemonic reveal hygiene ---
    private static readonly TimeSpan MnemonicRevealTtl = TimeSpan.FromSeconds(30);

    // --- Backup recovery file ---
    private const int MinRecoveryPasswordLength = 12;

    private readonly IPrefsStore _prefs;
    private readonly IPrefsSyncService _prefsSync;
    private readonly ICustodyService _custody;
    private readonly IPinService _pin;
    private readonly IProductClient _api;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private readonly IMnemonicBackupService _backup;
    private readonly IBackupFileService _backupFiles;
    private CancellationTokenSource? _mnemonicClearCts;

    private readonly TimeProvider _timeProvider;

    public ProfileViewModel(
        IPrefsStore prefs,
        IPrefsSyncService prefsSync,
        ICustodyService custody,
        IPinService pin,
        IProductClient api,
        INavigationService nav,
        IDialogService dialogs,
        ISettingsService settings,
        IAppSession session,
        IStepUpAuth stepUp,
        IMnemonicBackupService backup,
        IBackupFileService backupFiles,
        TimeProvider timeProvider,
        ICoraLineProvider coraLines)
    {
        _timeProvider = timeProvider;
        _prefs = prefs;
        _prefsSync = prefsSync;
        _custody = custody;
        _pin = pin;
        _api = api;
        _nav = nav;
        _dialogs = dialogs;
        _settings = settings;
        _session = session;
        _stepUp = stepUp;
        _backup = backup;
        _backupFiles = backupFiles;
        CoraLine = coraLines.GetLine("profile");
        foreach (string a in AppearanceChoices)
        {
            AppearanceOptions.Add(a);
        }

        foreach (string c in BaseCurrencyChoices)
        {
            BaseCurrencyOptions.Add(c);
        }

        foreach (string s in SendSpeedChoices)
        {
            SendSpeedOptions.Add(s);
        }
    }

    public ObservableCollection<VaultCardDto> Cards { get; } = new();

    public ObservableCollection<VaultBinaryDto> Binaries { get; } = new();

    public ObservableCollection<HomeSectionToggle> HomeSections { get; } = new();

    public ObservableCollection<CurrencyToggle> EnabledCurrencyRows { get; } = new();

    public ObservableCollection<string> AppearanceOptions { get; } = new();

    public ObservableCollection<string> BaseCurrencyOptions { get; } = new();

    public ObservableCollection<string> SendSpeedOptions { get; } = new();

    [ObservableProperty]
    private bool coraEnabled = true;

    [ObservableProperty]
    private string appearance = "dark";

    [ObservableProperty]
    private string baseCurrency = "USD";

    [ObservableProperty]
    private string defaultSendSpeed = "instant";

    [ObservableProperty]
    private int lockIdleSeconds = 120;

    [ObservableProperty]
    private string revealPin = string.Empty;

    [ObservableProperty]
    private string? mnemonicReveal;

    [ObservableProperty]
    private string backupPassword = string.Empty;

    [ObservableProperty]
    private string backupPasswordConfirm = string.Empty;

    [ObservableProperty]
    private string backupHint = string.Empty;

    [ObservableProperty]
    private bool isBackupBusy;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private bool showAdvanced;

    [ObservableProperty]
    private string apiEndpoint = string.Empty;

    [ObservableProperty]
    private bool useMockServices = true;

    [ObservableProperty]
    private bool valuesHiddenOnLaunch;

    [ObservableProperty]
    private bool combineAssets;

    [ObservableProperty]
    private string? activeCardId;

    [ObservableProperty]
    private string? activeCardLabel;

    [ObservableProperty]
    private VaultCardDto? selectedCard;

    /// <summary>
    /// Loads prefs and vault rows for Profile; vault API failures leave prior/empty lists.
    /// Use: High (Profile appearing). Scope: ProfileViewModel / product vault API.
    /// </summary>
    [RelayCommand]
    private async Task AppearingAsync()
    {
        _session.Touch();
        var prefs = await _prefs.LoadAsync();
        CoraEnabled = prefs.CoraEnabled;
        Appearance = prefs.Appearance;
        BaseCurrency = prefs.BaseCurrency;
        DefaultSendSpeed = prefs.DefaultSendSpeed;
        LockIdleSeconds = prefs.LockIdleSeconds;
        ValuesHiddenOnLaunch = prefs.ValuesHiddenOnLaunch;
        CombineAssets = prefs.AssetsLayout.Equals("combined", StringComparison.OrdinalIgnoreCase);
        ApiEndpoint = _settings.CipherBankEndpointBase;
#if DEBUG
        UseMockServices = _settings.UseMockServices;
#endif
        HomeSections.Clear();
        foreach (string key in prefs.HomeOrder)
        {
            bool visible = prefs.HomeVisible.TryGetValue(key, out bool v) && v;
            string label = SectionLabels.TryGetValue(key, out string? l) ? l : key;
            HomeSections.Add(new HomeSectionToggle(key, label, visible));
        }

        EnabledCurrencyRows.Clear();
        foreach (string symbol in CurrencyCatalog)
        {
            bool on = prefs.EnabledCurrencies.Contains(symbol, StringComparer.OrdinalIgnoreCase);
            EnabledCurrencyRows.Add(new CurrencyToggle(symbol, on));
        }

        await LoadVaultAsync();
        ActiveCardId = Preferences.Default.Get("pos_active_card", Cards.FirstOrDefault()?.CardId ?? string.Empty);
        SelectedCard = Cards.FirstOrDefault(c => c.CardId == ActiveCardId) ?? Cards.FirstOrDefault();
        ActiveCardLabel = SelectedCard is null ? null : $"{SelectedCard.Label} •••• {SelectedCard.Last4}";
    }

    /// <summary>
    /// Fetches vault cards/binaries; swallows offline/API failures so Appearing cannot throw.
    /// Use: High (Profile appearing). Scope: ProfileViewModel vault lists.
    /// </summary>
    private async Task LoadVaultAsync()
    {
        try
        {
            Cards.Clear();
            foreach (var c in await _api.GetVaultCardsAsync())
            {
                Cards.Add(c);
            }

            Binaries.Clear();
            foreach (var b in await _api.GetVaultBinariesAsync())
            {
                Binaries.Add(b);
            }
        }
        catch (HttpRequestException)
        {
            // Keep last-known / empty vault lists; offline chip is owned elsewhere.
        }
        catch (TaskCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (JsonException)
        {
        }
    }

    [RelayCommand]
    private async Task SavePrefsAsync()
    {
        _session.Touch();
        var prefs = await _prefs.LoadAsync();
        prefs.CoraEnabled = CoraEnabled;
        prefs.Appearance = Appearance;
        prefs.BaseCurrency = BaseCurrency;
        prefs.DefaultSendSpeed = DefaultSendSpeed;
        prefs.LockIdleSeconds = Math.Clamp(LockIdleSeconds, 30, 3600);
        prefs.ValuesHiddenOnLaunch = ValuesHiddenOnLaunch;
        prefs.AssetsLayout = CombineAssets ? "combined" : "separate";
        IEnumerable<string> enabled = EnabledCurrencyRows
            .Where(r => r.Enabled)
            .Select(r => r.Symbol);
        prefs.ReplaceEnabledCurrencies(enabled);
        if (prefs.EnabledCurrencies.Count == 0)
        {
            prefs.ReplaceEnabledCurrencies(UserPrefs.DefaultEnabledCurrencies);
        }

        foreach (var section in HomeSections)
        {
            prefs.HomeVisible[section.Key] = section.Visible;
        }

        bool pushed = await _prefsSync.SaveAndPushAsync(prefs);
        _session.IdleMs = prefs.LockIdleSeconds * 1000;
        Application.Current!.UserAppTheme = Appearance.Equals("light", StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Light
            : AppTheme.Dark;
        await _dialogs.ShowAlertAsync(
            "Saved",
            pushed ? "Preferences updated." : "Saved on device. Cloud sync failed — will retry later.");
    }

    [RelayCommand]
    private async Task RevealMnemonicAsync()
    {
        _session.Touch();
        if (!_custody.IsUnlocked)
        {
            await _dialogs.ShowAlertAsync("Locked", "Unlock custody first.");
            return;
        }

        if (!await _stepUp.RequireAsync(AuthReason.RevealKeys))
        {
            return;
        }

        MnemonicReveal = _custody.ExportMnemonic();
        RevealPin = string.Empty;
        ScheduleMnemonicClear();
    }

    /// <summary>
    /// Creates the ciphered recovery file for the unlocked wallet (step-up → password checks → Core
    /// <see cref="IMnemonicBackupService"/>), saves it to durable device storage and then offers the user a
    /// share copy. Recovery passwords never leave the entry fields.
    /// Use: Low (user-initiated export). Scope: Profile backup card.
    /// </summary>
    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        _session.Touch();
        try
        {
            if (!_custody.IsUnlocked)
            {
                await _dialogs.ShowAlertAsync("Locked", "Unlock custody first.");
                return;
            }

            if (!await _stepUp.RequireAsync(AuthReason.BackupExport))
            {
                return;
            }

            if (BackupPassword.Length < MinRecoveryPasswordLength)
            {
                await _dialogs.ShowAlertAsync(
                    "Password too short",
                    $"Recovery password must be at least {MinRecoveryPasswordLength} characters.");
                return;
            }

            if (!string.Equals(BackupPassword, BackupPasswordConfirm, StringComparison.Ordinal))
            {
                await _dialogs.ShowAlertAsync("Mismatch", "Recovery passwords do not match.");
                return;
            }

            string? mnemonic = _custody.ExportMnemonic();
            if (mnemonic is null)
            {
                await _dialogs.ShowAlertAsync("Locked", "Unlock custody first.");
                return;
            }

            IsBackupBusy = true;
            try
            {
                string? hint = string.IsNullOrWhiteSpace(BackupHint) ? null : BackupHint.Trim();
                byte[] file = await _backup.CreateBackupFileAsync(mnemonic, BackupPassword, hint);
                string fileName = $"cipherbank-recovery-{_timeProvider.GetUtcNow():yyyyMMdd-HHmmss}.cbr.json";
                string? savedTo = await _backupFiles.SaveRecoveryFileAsync(file, fileName);
                await OfferShareCopyAsync(file, fileName, savedTo);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowAlertAsync("Backup failed", ex.Message);
            }
            finally
            {
                IsBackupBusy = false;
            }
        }
        finally
        {
            ClearBackupFields();
        }
    }

    /// <summary>
    /// Tells the user where the export landed and lets them hand a copy to the OS share sheet. Sharing is
    /// opt-in so the encrypted phrase is never pushed into another app without being asked for.
    /// Use: Low (once per successful export). Scope: Profile backup card.
    /// </summary>
    private async Task OfferShareCopyAsync(byte[] file, string fileName, string? savedTo)
    {
        string where = savedTo is null ? "Recovery file created." : $"Saved to {savedTo}.";
        bool share = await _dialogs.ShowConfirmAsync(
            "Backup created",
            $"{where} Store it offline in a safe place — CipherBank never receives a copy. Share a copy now?",
            "Share",
            "Done");

        if (share)
        {
            await _backupFiles.ShareRecoveryFileAsync(file, fileName);
        }
    }

    /// <summary>Clears backup password/hint fields (call on leaving Profile too).</summary>
    public void ClearBackupFields()
    {
        BackupPassword = string.Empty;
        BackupPasswordConfirm = string.Empty;
        BackupHint = string.Empty;
    }

    [RelayCommand]
    private async Task AddDemoCardAsync()
    {
        _session.Touch();
        VaultCardDto card = await _api.AddVaultCardAsync(
            new VaultCardDto
            {
                Last4 = "0001",
                Brand = "visa",
                Label = "Demo card",
                HardwareTest = true,
            },
            Guid.NewGuid().ToString("N"));
        Cards.Add(card);
    }

    [RelayCommand]
    private async Task RemoveCardAsync(VaultCardDto? card)
    {
        _session.Touch();
        VaultCardDto? cardToRemove = card ?? SelectedCard;
        if (cardToRemove is null)
        {
            return;
        }

        bool removingActivePosCard = string.Equals(cardToRemove.CardId, ActiveCardId, StringComparison.Ordinal);
        if (removingActivePosCard && !await _stepUp.RequireAsync(AuthReason.PosAuthorize))
        {
            return;
        }

        bool confirmed = await _dialogs.ShowConfirmAsync(
            "Remove card",
            $"Remove {cardToRemove.Label} •••• {cardToRemove.Last4} from the vault?",
            "Remove",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        await _api.DeleteVaultCardAsync(cardToRemove.CardId);
        Cards.Remove(cardToRemove);
        if (removingActivePosCard)
        {
            ActiveCardId = Cards.FirstOrDefault()?.CardId;
            SelectedCard = Cards.FirstOrDefault();
            Preferences.Default.Set("pos_active_card", ActiveCardId ?? string.Empty);
        }
    }

    /// <summary>Clears on-screen mnemonic (call when leaving Profile).</summary>
    public void ClearMnemonicReveal()
    {
        _mnemonicClearCts?.Cancel();
        _mnemonicClearCts?.Dispose();
        _mnemonicClearCts = null;
        MnemonicReveal = null;
        RevealPin = string.Empty;
    }

    private void ScheduleMnemonicClear()
    {
        _mnemonicClearCts?.Cancel();
        _mnemonicClearCts?.Dispose();
        _mnemonicClearCts = new CancellationTokenSource();
        CancellationToken token = _mnemonicClearCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(MnemonicRevealTtl, token).ConfigureAwait(false);
                MainThread.BeginInvokeOnMainThread(ClearMnemonicReveal);
            }
            catch (OperationCanceledException)
            {
                // superseded
            }
        }, token);
    }

    partial void OnSelectedCardChanged(VaultCardDto? value)
    {
        if (value is null)
        {
            return;
        }

        ActiveCardId = value.CardId;
        ActiveCardLabel = $"{value.Label} •••• {value.Last4}";
        Preferences.Default.Set("pos_active_card", value.CardId);
    }

    [RelayCommand]
    private void ToggleAdvanced() => ShowAdvanced = !ShowAdvanced;

    [RelayCommand]
    private Task OpenPosLabAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.PosLab);
    }

    /// <summary>
    /// Opens the Change PIN surface from Profile → Security.
    /// Use: Low (user-initiated PIN change). Scope: this Profile page instance.
    /// </summary>
    [RelayCommand]
    private Task OpenChangePinAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.ChangePin);
    }

    [RelayCommand]
    private async Task SaveAdvancedAsync()
    {
        _settings.CipherBankEndpointBase = ApiEndpoint;
#if DEBUG
        _settings.UseMockServices = UseMockServices;
#endif
        await _dialogs.ShowAlertAsync(
            "Saved",
            "Advanced settings stored. Restart the app for API endpoint and mock-mode changes to take effect.");
    }

    [RelayCommand]
    private void Lock()
    {
        _session.Lock();
        _ = _nav.GoToAsync(Routes.Unlock);
    }

    /// <summary>
    /// Cancels any pending mnemonic auto-clear when Profile leaves DI scope.
    /// Use: Medium (page teardown). Scope: this ProfileViewModel instance.
    /// </summary>
    public void Dispose()
    {
        ClearMnemonicReveal();
        GC.SuppressFinalize(this);
    }
}
