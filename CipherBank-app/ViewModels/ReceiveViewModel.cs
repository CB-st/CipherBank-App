// <copyright file="ReceiveViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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

/// <summary>Receive address + QR.</summary>
public partial class ReceiveViewModel : ObservableObject
{
    private readonly IProductApi _api;
    private readonly ICustodyService _custody;
    private readonly IWalletRepository _wallets;
    private readonly IAppSession _session;

    public ReceiveViewModel(IProductApi api, ICustodyService custody, IWalletRepository wallets, IAppSession session)
    {
        _api = api;
        _custody = custody;
        _wallets = wallets;
        _session = session;
        CoraLine = CoraLines.For("receive");
    }

    [ObservableProperty]
    private string asset = "BTC";

    [ObservableProperty]
    private string amount = string.Empty;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string uriText = string.Empty;

    [ObservableProperty]
    private ImageSource? qrImage;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        _session.Touch();
        string? mnemonic = _custody.ExportMnemonic();
        if (mnemonic is not null && AddressDerive.IsDerivable(Asset))
        {
            var derived = AddressDerive.Derive(Asset, mnemonic, 0);
            if (derived is not null)
            {
                Address = derived.Address;
            }
        }

        if (string.IsNullOrEmpty(Address))
        {
            var recv = await _api.GetReceiveAsync(Asset);
            Address = recv.Address;
        }

        UriText = PaymentUri.Build(Asset, Address, string.IsNullOrWhiteSpace(Amount) ? null : Amount);
        byte[] png = QrCodeGenerator.ToPngBytes(UriText);
        QrImage = ImageSource.FromStream(() => new MemoryStream(png));
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
            DateTimeOffset.UtcNow));
        Address = derived.Address;
        await LoadAsync();
    }
}
