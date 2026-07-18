// <copyright file="ConvertViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>FX convert with client-side quote lock (Phase C ready).</summary>
public partial class ConvertViewModel : ObservableObject
{
    private readonly IProductApi _api;
    private readonly IDialogService _dialogs;
    private readonly IAppSession _session;
    private QuoteDto? _lockedQuote;

    public ConvertViewModel(IProductApi api, IDialogService dialogs, IAppSession session)
    {
        _api = api;
        _dialogs = dialogs;
        _session = session;
        CoraLine = CoraLines.For("convert");
    }

    [ObservableProperty]
    private string fromAsset = "BTC";

    [ObservableProperty]
    private string toAsset = "USD";

    [ObservableProperty]
    private string amount = "0.01";

    [ObservableProperty]
    private string? rateText;

    [ObservableProperty]
    private string? lockCountdown;

    [ObservableProperty]
    private bool hasValidLock;

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private string? status;

    [ObservableProperty]
    private bool isBusy;

    private bool IsQuoteFresh()
        => _lockedQuote is not null
           && _lockedQuote.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [RelayCommand]
    private async Task LockQuoteAsync()
    {
        _session.Touch();
        IsBusy = true;
        try
        {
            _lockedQuote = await _api.GetQuoteAsync(FromAsset, ToAsset);
            long remainingMs = _lockedQuote.ExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            HasValidLock = remainingMs > 0;
            RateText = $"1 {_lockedQuote.From} = {_lockedQuote.Rate} {_lockedQuote.To}";
            LockCountdown = HasValidLock ? $"{remainingMs / 1000}s remaining" : "Expired";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        _session.Touch();
        if (!_session.IsUnlocked)
        {
            await _dialogs.ShowAlertAsync("Locked", "Unlock custody before converting.");
            return;
        }

        if (!IsQuoteFresh())
        {
            await _dialogs.ShowAlertAsync("Quote", "Lock a fresh quote first.");
            return;
        }

        if (!decimal.TryParse(Amount, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal amt) || amt <= 0)
        {
            await _dialogs.ShowAlertAsync("Amount", "Enter a positive amount.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _api.ConvertAsync(FromAsset, ToAsset, Amount, Guid.NewGuid().ToString("N"));
            Status = $"Convert {result.Id}: {result.Status}";
            await _dialogs.ShowAlertAsync("Convert", Status);
            HasValidLock = false;
            _lockedQuote = null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
