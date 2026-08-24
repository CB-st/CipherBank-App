// <copyright file="ConvertViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using System.Globalization;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>FX convert with public /iquote locks, asset pickers, swap, and countdown.</summary>
public partial class ConvertViewModel : ObservableObject, IDisposable
{
    private readonly IProductClient _api;
    private readonly IPublicQuoteService _publicQuotes;
    private readonly IDialogService _dialogs;
    private readonly IAppSession _session;
    private readonly IStepUpAuth _stepUp;
    private readonly IStreamHub _streamHub;
    private QuoteDto? _lockedQuote;
    private CancellationTokenSource? _tickCts;
    private bool _streamHooked;
    private bool _assetsLoaded;

    private readonly TimeProvider _timeProvider;

    public ConvertViewModel(
        IProductClient api,
        IPublicQuoteService publicQuotes,
        IDialogService dialogs,
        IAppSession session,
        IStepUpAuth stepUp,
        IStreamHub streamHub,
        TimeProvider timeProvider,
        ICoraLineProvider coraLines)
    {
        _timeProvider = timeProvider;
        _api = api;
        _publicQuotes = publicQuotes;
        _dialogs = dialogs;
        _session = session;
        _stepUp = stepUp;
        _streamHub = streamHub;
        CoraLine = coraLines.GetLine("convert");
        foreach (string symbol in FallbackAssets())
        {
            Assets.Add(symbol);
        }

        EnsureStreamHooked();
        _ = LoadAssetsAsync();
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

    private void OnStreamEvent(object? sender, StreamEventArgs e)
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

    private async Task LoadAssetsAsync()
    {
        if (_assetsLoaded)
        {
            return;
        }

        try
        {
            IReadOnlyList<string> currencies = await _publicQuotes.GetCurrenciesAsync();
            if (currencies.Count == 0)
            {
                return;
            }

            void Apply()
            {
                Assets.Clear();
                foreach (string symbol in currencies.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                {
                    Assets.Add(symbol);
                }

                if (!Assets.Contains(FromAsset))
                {
                    FromAsset = Assets.FirstOrDefault() ?? "BTC";
                }

                if (!Assets.Contains(ToAsset) || ToAsset.Equals(FromAsset, StringComparison.OrdinalIgnoreCase))
                {
                    ToAsset = Assets.FirstOrDefault(s => !s.Equals(FromAsset, StringComparison.OrdinalIgnoreCase))
                              ?? "USD";
                }

                _assetsLoaded = true;
            }

            if (MainThread.IsMainThread)
            {
                Apply();
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(Apply);
            }
        }
        catch
        {
            // Keep registry fallback list.
        }
    }

    private async Task RefreshPreviewRateAsync()
    {
        try
        {
            if (!TryParseAmount(out decimal amt) || amt <= 0)
            {
                amt = 1m;
            }

            var quote = await _publicQuotes.GetInverseQuoteAsync(FromAsset, amt, ToAsset);
            string label = $"1 {quote.InputCurrency} ≈ {FormatRate(quote.Rate)} {quote.OutputCurrency} (live)";
            if (MainThread.IsMainThread)
            {
                RateText = label;
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => RateText = label);
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
    private bool isIndicative;

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

    partial void OnFromAssetChanged(string value)
    {
        InvalidateLockedQuote();
        RefreshInfoRows();
    }

    partial void OnToAssetChanged(string value)
    {
        InvalidateLockedQuote();
        RefreshInfoRows();
    }

    partial void OnAmountChanged(string value) => InvalidateLockedQuote();

    private static List<string> FallbackAssets()
    {
        var list = WalletRegistry.All()
            .Select(m => m.Symbol)
            .Where(CurrencySymbolMap.IsSupported)
            .ToList();
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
        SettlementText = IsIndicative
            ? "Mock settle until server /convert"
            : "Instant";
    }

    /// <summary>
    /// Drops a locked/indicative quote when From/To/Amount change so Convert cannot settle a stale lock.
    /// Use: High (asset/amount edits). Scope: ConvertViewModel quote state.
    /// </summary>
    private void InvalidateLockedQuote()
    {
        HasValidLock = false;
        IsIndicative = false;
        _lockedQuote = null;
        LockCountdown = null;
        RateText = null;
        _tickCts?.Cancel();
    }

    private bool IsQuoteFresh()
        => _lockedQuote is not null
           && _lockedQuote.ExpiresAt > _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    private bool TryParseAmount(out decimal amt)
        => decimal.TryParse(
            Amount,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amt);

    private static string FormatRate(decimal rate)
        => rate.ToString("0.########", CultureInfo.InvariantCulture);

    [RelayCommand]
    private void SwapAssets()
    {
        (FromAsset, ToAsset) = (ToAsset, FromAsset);
    }

    [RelayCommand]
    private async Task LockQuoteAsync()
    {
        _session.Touch();
        if (!TryParseAmount(out decimal amt) || amt <= 0)
        {
            await _dialogs.ShowAlertAsync("Amount", "Enter a positive amount.");
            return;
        }

        IsBusy = true;
        try
        {
            var pub = await _publicQuotes.GetInverseQuoteAsync(FromAsset, amt, ToAsset);
            long now = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            _lockedQuote = IndicativeQuoteMapper.ToQuoteDto(pub, now);
            IsIndicative = true;
            RateText = $"1 {_lockedQuote.From} = {FormatRate(pub.Rate)} {_lockedQuote.To}";
            RefreshInfoRows();
            StartCountdown();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowAlertAsync("Quote", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartCountdown()
    {
        _tickCts?.Cancel();
        _tickCts?.Dispose();
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

        long remainingMs = _lockedQuote.ExpiresAt - _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        HasValidLock = remainingMs > 0;
        string kind = IsIndicative ? "Indicative" : "Locked";
        LockCountdown = HasValidLock ? $"● {kind} · {remainingMs / 1000}s" : "Expired";
        if (!HasValidLock)
        {
            _lockedQuote = null;
            IsIndicative = false;
            RefreshInfoRows();
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
            await _dialogs.ShowAlertAsync("Quote", "Get a fresh indicative quote first.");
            return;
        }

        if (!TryParseAmount(out decimal amt) || amt <= 0)
        {
            await _dialogs.ShowAlertAsync("Amount", "Enter a positive amount.");
            return;
        }

        IsBusy = true;
        try
        {
            // Settlement remains on product/mock path until server POST /convert ships.
            var result = await _api.ConvertAsync(FromAsset, ToAsset, Amount, Guid.NewGuid().ToString("N"));
            Status = $"Convert {result.Id}: {result.Status}";
            await _dialogs.ShowAlertAsync("Convert", Status);
            HasValidLock = false;
            IsIndicative = false;
            _lockedQuote = null;
            _tickCts?.Cancel();
            LockCountdown = null;
            RefreshInfoRows();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Cancels the quote countdown and unhooks stream listeners when the convert page leaves DI scope.
    /// Use: Medium (page teardown). Scope: this ConvertViewModel instance.
    /// </summary>
    public void Dispose()
    {
        _tickCts?.Cancel();
        _tickCts?.Dispose();
        _tickCts = null;
        if (_streamHooked)
        {
            _streamHub.EventReceived -= OnStreamEvent;
            _streamHooked = false;
        }

        GC.SuppressFinalize(this);
    }
}
