// <copyright file="ConvertViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>FX convert with asset pickers, swap, and quote countdown.</summary>
public partial class ConvertViewModel : ObservableObject
{
    private readonly IProductApi _api;
    private readonly IDialogService _dialogs;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private readonly IStreamHub _streamHub;
    private QuoteDto? _lockedQuote;
    private CancellationTokenSource? _tickCts;
    private bool _streamHooked;

    public ConvertViewModel(
        IProductApi api,
        IDialogService dialogs,
        IAppSession session,
        IStepUpAuth stepUp,
        IStreamHub streamHub)
    {
        _api = api;
        _dialogs = dialogs;
        _session = session;
        _stepUp = stepUp;
        _streamHub = streamHub;
        CoraLine = CoraLines.For("convert");
        foreach (string symbol in BuildAssetList())
        {
            Assets.Add(symbol);
        }

        EnsureStreamHooked();
    }

    private void EnsureStreamHooked()
    {
        if (_streamHooked)
        {
            return;
        }

        _streamHub.EventReceived += OnStreamEvent;
        _streamHooked = true;
    }

    private void OnStreamEvent(object? sender, StreamEvent e)
    {
        if (!e.Type.Equals("RATE.TICK", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (HasValidLock && IsQuoteFresh())
        {
            return;
        }

        _ = RefreshPreviewRateAsync();
    }

    private async Task RefreshPreviewRateAsync()
    {
        try
        {
            var quote = await _api.GetQuoteAsync(FromAsset, ToAsset);
            if (MainThread.IsMainThread)
            {
                RateText = $"1 {quote.From} ≈ {quote.Rate} {quote.To} (live)";
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    RateText = $"1 {quote.From} ≈ {quote.Rate} {quote.To} (live)";
                });
            }
        }
        catch
        {
            // Preview refresh is best-effort.
        }
    }

    public ObservableCollection<string> Assets { get; } = new();

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

    [ObservableProperty]
    private string feeText = "$0.00 we cover it";

    [ObservableProperty]
    private string privacyText = "Private by default";

    [ObservableProperty]
    private string settlementText = "Instant";

    partial void OnFromAssetChanged(string value) => RefreshInfoRows();

    partial void OnToAssetChanged(string value) => RefreshInfoRows();

    private static List<string> BuildAssetList()
    {
        var list = WalletRegistry.All().Select(m => m.Symbol).ToList();
        if (!list.Contains("USD", StringComparer.OrdinalIgnoreCase))
        {
            list.Insert(0, "USD");
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
    }

    private void RefreshInfoRows()
    {
        bool xmr = FromAsset.Equals("XMR", StringComparison.OrdinalIgnoreCase)
                   || ToAsset.Equals("XMR", StringComparison.OrdinalIgnoreCase);
        PrivacyText = xmr ? "Shielded swap" : "Private by default";
        FeeText = "$0.00 we cover it";
        SettlementText = "Instant";
    }

    private bool IsQuoteFresh()
        => _lockedQuote is not null
           && _lockedQuote.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [RelayCommand]
    private void SwapAssets()
    {
        (FromAsset, ToAsset) = (ToAsset, FromAsset);
        HasValidLock = false;
        _lockedQuote = null;
        LockCountdown = null;
        RateText = null;
        _tickCts?.Cancel();
    }

    [RelayCommand]
    private async Task LockQuoteAsync()
    {
        _session.Touch();
        IsBusy = true;
        try
        {
            _lockedQuote = await _api.GetQuoteAsync(FromAsset, ToAsset);
            RateText = $"1 {_lockedQuote.From} = {_lockedQuote.Rate} {_lockedQuote.To}";
            RefreshInfoRows();
            StartCountdown();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartCountdown()
    {
        _tickCts?.Cancel();
        _tickCts = new CancellationTokenSource();
        CancellationToken ct = _tickCts.Token;
        TickOnce();
        _ = TickLoopAsync(ct);
    }

    private void TickOnce()
    {
        if (_lockedQuote is null)
        {
            HasValidLock = false;
            LockCountdown = null;
            return;
        }

        long remainingMs = _lockedQuote.ExpiresAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        HasValidLock = remainingMs > 0;
        LockCountdown = HasValidLock ? $"{remainingMs / 1000}s remaining" : "Expired";
        if (!HasValidLock)
        {
            _lockedQuote = null;
        }
    }

    private async Task TickLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct).ConfigureAwait(false);
                if (MainThread.IsMainThread)
                {
                    TickOnce();
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(TickOnce);
                }

                if (!HasValidLock)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when locking a new quote or leaving.
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

        if (!await _stepUp.RequireAsync(AuthReason.Convert))
        {
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
            _tickCts?.Cancel();
            LockCountdown = null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
