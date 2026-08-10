// <copyright file="ReceiveViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Receive address + QR with asset chips and derivation path.</summary>
public partial class ReceiveViewModel : ObservableObject
{
    private readonly IProductClient _api;
    private readonly ICustodyService _custody;
    private readonly IWalletRepository _wallets;
    private readonly IAppSession _session;

    private readonly TimeProvider _timeProvider;

    public ReceiveViewModel(
        IProductClient api,
        ICustodyService custody,
        IWalletRepository wallets,
        IAppSession session,
        TimeProvider timeProvider,
        ICoraLineProvider coraLines)
    {
        _timeProvider = timeProvider;
        _api = api;
        _custody = custody;
        _wallets = wallets;
        _session = session;
        CoraLine = coraLines.GetLine("receive");
        foreach (string symbol in new[] { "BTC", "ETH", "USD" })
        {
            AssetChips.Add(new AssetChip(symbol, symbol == "BTC"));
        }

        foreach (var mod in WalletRegistry.All())
        {
            if (AssetChips.Any(c => c.Symbol.Equals(mod.Symbol, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            AssetChips.Add(new AssetChip(mod.Symbol, false));
        }
    }

    public ObservableCollection<AssetChip> AssetChips { get; } = new();

    [ObservableProperty]
    private string asset = "BTC";

    [ObservableProperty]
    private string amount = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string uriText = string.Empty;

    [ObservableProperty]
    private string? derivationPath;

    public bool HasDerivationPath => !string.IsNullOrEmpty(DerivationPath);

    partial void OnDerivationPathChanged(string? value) => OnPropertyChanged(nameof(HasDerivationPath));

    [ObservableProperty]
    private ImageSource? qrImage;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [RelayCommand]
    private async Task SelectAssetAsync(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return;
        }

        Asset = symbol.ToUpperInvariant();
        foreach (var chip in AssetChips)
        {
            chip.Selected = chip.Symbol.Equals(Asset, StringComparison.OrdinalIgnoreCase);
        }

        await LoadAsync();
    }

    /// <summary>
    /// Resolves a receive address (local derive first, API fallback) and QR URI.
    /// Use: High (Receive appearing / asset chip). Scope: ReceiveViewModel.
    /// </summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        _session.Touch();
        ClearReceivePresentation();
        string? mnemonic = _custody.ExportMnemonic();
        if (mnemonic is not null && AddressDerive.IsDerivable(Asset))
        {
            var derived = AddressDerive.Derive(Asset, mnemonic, 0);
            if (derived is not null)
            {
                Address = derived.Address;
                DerivationPath = derived.Path;
            }
        }

        if (string.IsNullOrEmpty(Address) || !AddressDerive.IsDerivable(Asset))
        {
            await LoadReceiveFromApiAsync();
        }

        // Prefer path from matching local wallet when present.
        var local = (await _wallets.ListAsync())
            .FirstOrDefault(w => w.Symbol.Equals(Asset, StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrEmpty(w.Path));
        if (local is not null)
        {
            Address = local.Address ?? Address;
            DerivationPath = local.Path;
        }

        ApplyReceivePresentation();
    }

    /// <summary>
    /// Fetches a server receive address when local derive is unavailable; ignores offline failures.
    /// Use: Medium (API fallback). Scope: ReceiveViewModel / product receive API.
    /// </summary>
    private async Task LoadReceiveFromApiAsync()
    {
        try
        {
            var recv = await _api.GetReceiveAsync(Asset);
            Address = recv.Address;
            if (!AddressDerive.IsDerivable(Asset))
            {
                DerivationPath = null;
            }
        }
        catch (HttpRequestException)
        {
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
    private async Task CopyAsync()
        => await Clipboard.Default.SetTextAsync(Address);

    [RelayCommand]
    private async Task ShareAsync()
        => await Share.Default.RequestAsync(new ShareTextRequest { Text = UriText, Title = "Receive" });

    [RelayCommand]
    private async Task DeriveNewAsync()
    {
        string? mnemonic = _custody.ExportMnemonic();
        if (mnemonic is null || !AddressDerive.IsDerivable(Asset))
        {
            return;
        }

        var existing = await _wallets.ListAsync();
        int next = existing.Count(w => w.Symbol.Equals(Asset, StringComparison.OrdinalIgnoreCase));
        var derived = AddressDerive.Derive(Asset, mnemonic, next)!;
        await _wallets.UpsertAsync(new LocalWalletRow(
            Guid.NewGuid().ToString("N"),
            Asset,
            $"{Asset} #{next}",
            derived.Address,
            derived.Path,
            derived.AccountIndex,
            "derived",
            _timeProvider.GetUtcNow()));
        Address = derived.Address;
        DerivationPath = derived.Path;
        ApplyReceivePresentation();
    }

    /// <summary>
    /// Drops prior asset address/QR so a failed lookup cannot reuse the last asset.
    /// Use: High (every LoadAsync). Scope: ReceiveViewModel bindable state.
    /// </summary>
    private void ClearReceivePresentation()
    {
        Address = string.Empty;
        DerivationPath = null;
        UriText = string.Empty;
        QrImage = null;
    }

    /// <summary>
    /// Builds payment URI + QR only when an address resolved for the selected asset.
    /// Use: High (LoadAsync / DeriveNewAsync). Scope: ReceiveViewModel bindable state.
    /// </summary>
    private void ApplyReceivePresentation()
    {
        if (string.IsNullOrEmpty(Address))
        {
            UriText = string.Empty;
            QrImage = null;
            return;
        }

        UriText = PaymentUri.Build(Asset, Address, string.IsNullOrWhiteSpace(Amount) ? null : Amount);
        byte[] png = QrCodeGenerator.ToPngBytes(UriText);
        QrImage = ImageSource.FromStream(() => new MemoryStream(png));
    }
}
