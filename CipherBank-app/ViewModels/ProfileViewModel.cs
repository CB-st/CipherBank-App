// <copyright file="ProfileViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
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

/// <summary>Home section toggle row.</summary>
public partial class HomeSectionToggle : ObservableObject
{
    public HomeSectionToggle(string key, string label, bool visible)
    {
        Key = key;
        Label = label;
        Visible = visible;
    }

    public string Key { get; }

    public string Label { get; }

    [ObservableProperty]
    private bool visible;
}

/// <summary>Profile / prefs / vault — Phase D polished.</summary>
public partial class ProfileViewModel : ObservableObject
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

    // --- Mnemonic reveal hygiene ---
    private static readonly TimeSpan MnemonicRevealTtl = TimeSpan.FromSeconds(30);

    private readonly IPrefsStore _prefs;
    private readonly ICustodyService _custody;
    private readonly IPinService _pin;
    private readonly IProductApi _api;
    private readonly INavigationService _nav;
    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private CancellationTokenSource? _mnemonicClearCts;

    public ProfileViewModel(
        IPrefsStore prefs,
        ICustodyService custody,
        IPinService pin,
        IProductApi api,
        INavigationService nav,
        IDialogService dialogs,
        ISettingsService settings,
        IAppSession session,
        IStepUpAuth stepUp)
    {
        _prefs = prefs;
        _custody = custody;
        _pin = pin;
        _api = api;
        _nav = nav;
        _dialogs = dialogs;
        _settings = settings;
        _session = session;
        _stepUp = stepUp;
        CoraLine = CoraLines.For("profile");
    }

    public ObservableCollection<VaultCardDto> Cards { get; } = new();

    public ObservableCollection<VaultBinaryDto> Binaries { get; } = new();

    public ObservableCollection<HomeSectionToggle> HomeSections { get; } = new();

    [ObservableProperty]
    private bool coraEnabled = true;

    [ObservableProperty]
    private string appearance = "dark";

    [ObservableProperty]
    private string baseCurrency = "USD";

    [ObservableProperty]
    private int lockIdleSeconds = 120;

    [ObservableProperty]
    private string revealPin = string.Empty;

    [ObservableProperty]
    private string? mnemonicReveal;

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

    [RelayCommand]
    private async Task AppearingAsync()
    {
        _session.Touch();
        var prefs = await _prefs.LoadAsync();
        CoraEnabled = prefs.CoraEnabled;
        Appearance = prefs.Appearance;
        BaseCurrency = prefs.BaseCurrency;
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

        ActiveCardId = Preferences.Default.Get("pos_active_card", Cards.FirstOrDefault()?.CardId ?? string.Empty);
        SelectedCard = Cards.FirstOrDefault(c => c.CardId == ActiveCardId) ?? Cards.FirstOrDefault();
        ActiveCardLabel = SelectedCard is null ? null : $"{SelectedCard.Label} •••• {SelectedCard.Last4}";
    }

    [RelayCommand]
    private async Task SavePrefsAsync()
    {
        _session.Touch();
        var prefs = await _prefs.LoadAsync();
        prefs.CoraEnabled = CoraEnabled;
        prefs.Appearance = Appearance;
        prefs.BaseCurrency = BaseCurrency;
        prefs.LockIdleSeconds = Math.Clamp(LockIdleSeconds, 30, 3600);
        prefs.ValuesHiddenOnLaunch = ValuesHiddenOnLaunch;
        prefs.AssetsLayout = CombineAssets ? "combined" : "separate";
        foreach (var section in HomeSections)
        {
            prefs.HomeVisible[section.Key] = section.Visible;
        }

        await _prefs.SaveAsync(prefs);
        _session.IdleMs = prefs.LockIdleSeconds * 1000;
        Application.Current!.UserAppTheme = Appearance.Equals("light", StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Light
            : AppTheme.Dark;
        await _dialogs.ShowAlertAsync("Saved", "Preferences updated.");
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

    /// <summary>Clears on-screen mnemonic (call when leaving Profile).</summary>
    public void ClearMnemonicReveal()
    {
        _mnemonicClearCts?.Cancel();
        _mnemonicClearCts = null;
        MnemonicReveal = null;
        RevealPin = string.Empty;
    }

    private void ScheduleMnemonicClear()
    {
        _mnemonicClearCts?.Cancel();
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
        });
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

    [RelayCommand]
    private async Task SaveAdvancedAsync()
    {
        _settings.CipherBankEndpointBase = ApiEndpoint;
#if DEBUG
        _settings.UseMockServices = UseMockServices;
#endif
        await _dialogs.ShowAlertAsync("Saved", "Advanced settings updated.");
    }

    [RelayCommand]
    private void Lock()
    {
        _session.Lock();
        _ = _nav.GoToAsync(Routes.Unlock);
    }
}
