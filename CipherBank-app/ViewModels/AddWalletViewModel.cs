// <copyright file="AddWalletViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Add derived, watch, or managed server wallet.</summary>
public partial class AddWalletViewModel : ObservableObject
{
    private readonly ICustodyService _custody;
    private readonly IWalletRepository _wallets;
    private readonly IDialogService _dialogs;
    private readonly IProductClient _api;

    private readonly TimeProvider _timeProvider;

    public AddWalletViewModel(
        ICustodyService custody,
        IWalletRepository wallets,
        IDialogService dialogs,
        IProductClient api,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _custody = custody;
        _wallets = wallets;
        _dialogs = dialogs;
        _api = api;
        AvailableSymbols = WalletRegistry.All().Select(m => m.Symbol).ToList();
        Symbol = AvailableSymbols.Count > 0 ? AvailableSymbols[0] : "BTC";
        RefreshModes();
    }

    public IReadOnlyList<string> AvailableSymbols { get; }

    public List<string> ModeLabels { get; } = new();

    [ObservableProperty]
    private string symbol = "BTC";

    [ObservableProperty]
    private string selectedMode = "Derive";

    [ObservableProperty]
    private string watchAddress = string.Empty;

    [ObservableProperty]
    private string label = string.Empty;

    partial void OnSymbolChanged(string value) => RefreshModes();

    private void RefreshModes()
    {
        ModeLabels.Clear();
        foreach (var mode in WalletRegistry.Get(Symbol).AddModes)
        {
            ModeLabels.Add(mode.ToString());
        }

        SelectedMode = ModeLabels.FirstOrDefault() ?? "Watch";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var module = WalletRegistry.Get(Symbol);
        if (!Enum.TryParse(SelectedMode, true, out WalletUiMode mode))
        {
            mode = WalletUiMode.Watch;
        }

        if (mode == WalletUiMode.Derive)
        {
            string? mnemonic = _custody.ExportMnemonic();
            if (mnemonic is null)
            {
                await _dialogs.ShowAlertAsync("Locked", "Unlock custody to derive an address.");
                return;
            }

            if (!module.CanDerive)
            {
                await _dialogs.ShowAlertAsync("Unsupported", $"{Symbol} does not support on-device derive yet.");
                return;
            }

            var existing = await _wallets.ListAsync();
            int next = existing.Count(w => w.Symbol.Equals(Symbol, StringComparison.OrdinalIgnoreCase));
            var derived = AddressDerive.Derive(Symbol, mnemonic, next)!;
            await _wallets.UpsertAsync(new LocalWalletRow(
                Guid.NewGuid().ToString("N"),
                Symbol.ToUpperInvariant(),
                string.IsNullOrWhiteSpace(Label) ? $"{Symbol} #{next}" : Label,
                derived.Address,
                derived.Path,
                derived.AccountIndex,
                "derived",
                _timeProvider.GetUtcNow()));
        }
        else if (mode == WalletUiMode.Watch)
        {
            if (!AddressValidate.IsValid(Symbol, WatchAddress))
            {
                await _dialogs.ShowAlertAsync("Invalid", "That watch address does not look valid.");
                return;
            }

            await _wallets.UpsertAsync(new LocalWalletRow(
                Guid.NewGuid().ToString("N"),
                Symbol.ToUpperInvariant(),
                string.IsNullOrWhiteSpace(Label) ? $"{Symbol} watch" : Label,
                WatchAddress.Trim(),
                null,
                0,
                "watch",
                _timeProvider.GetUtcNow()));
        }
        else if (mode == WalletUiMode.Managed)
        {
            try
            {
                CreateWalletResultDto result = await _api.CreateWalletAsync(new CreateWalletRequestDto
                {
                    Symbol = Symbol.ToUpperInvariant(),
                    Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
                    Mode = "managed",
                });
                await _wallets.UpsertAsync(new LocalWalletRow(
                    result.WalletId,
                    result.Symbol,
                    result.Label,
                    result.Address,
                    null,
                    0,
                    "managed",
                    _timeProvider.GetUtcNow()));
                await _dialogs.ShowAlertAsync("Managed wallet", "Server wallet created — spend key stays with CipherBank.");
            }
            catch (Exception ex)
            {
                await _dialogs.ShowAlertAsync("Failed", ex.Message);
            }

            return;
        }
        else
        {
            // Unmanaged / other — local placeholder only (no spend key stored).
            await _wallets.UpsertAsync(new LocalWalletRow(
                Guid.NewGuid().ToString("N"),
                Symbol.ToUpperInvariant(),
                string.IsNullOrWhiteSpace(Label) ? $"{Symbol} {mode}" : Label,
                WatchAddress.Trim().Length > 0 ? WatchAddress.Trim() : null,
                null,
                0,
                mode.ToString().ToLowerInvariant(),
                _timeProvider.GetUtcNow()));
        }

        await _dialogs.ShowAlertAsync("Saved", "Wallet added.");
    }
}
