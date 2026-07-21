// <copyright file="HomeViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Charts;
using CipherBank_app.Constants;
using CipherBank_app.Controls;
using CipherBank_app.Cora;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherBank_app.ViewModels;

/// <summary>Home portfolio shell — F2 section prefs + hide balances.</summary>
public partial class HomeViewModel : ObservableObject
{
    private static readonly Color HoldingsAccent = Color.FromArgb("#3FA46A");
    private static readonly Color LocalAccent = Color.FromArgb("#F2C14E");

    private static readonly Color[] SeriesColors =
    {
        Color.FromArgb("#F2C14E"),
        Color.FromArgb("#7B4DFF"),
        Color.FromArgb("#3FA46A"),
    };

    private readonly IProductApi _api;
    private readonly IPrefsStore _prefs;
    private readonly IWalletRepository _wallets;
    private readonly INavigationService _nav;
    private readonly IAppSession _session;
    private readonly IStreamHub _streamHub;
    private readonly IStreamService _stream;
    private readonly IRatesCache _ratesCache;
    private readonly IMarketRepository _marketRepository;
    private readonly ISyncJobQueue _syncJobQueue;
    private readonly IPublicQuoteService _publicQuotes;
    private readonly EventDebouncer _refreshDebounce = new(TimeSpan.FromSeconds(1));
    private IReadOnlyCollection<string> _enabledCurrencies = UserPrefs.DefaultEnabledCurrencies;
    private bool _streamHooked;
    private bool _lastPortfolioOk;
    private string _rawTotalUsd = "—";
    private string _rawChange24H = "—";

    public HomeViewModel(
        IProductApi api,
        IPrefsStore prefs,
        IWalletRepository wallets,
        INavigationService nav,
        IAppSession session,
        IStreamHub streamHub,
        IStreamService stream,
        IRatesCache ratesCache,
        IMarketRepository marketRepository,
        ISyncJobQueue syncJobQueue,
        IPublicQuoteService publicQuotes)
    {
        _api = api;
        _prefs = prefs;
        _wallets = wallets;
        _nav = nav;
        _session = session;
        _streamHub = streamHub;
        _stream = stream;
        _ratesCache = ratesCache;
        _marketRepository = marketRepository;
        _syncJobQueue = syncJobQueue;
        _publicQuotes = publicQuotes;
        CoraLine = CoraLines.For("home");
        RefreshOnline();
    }

    public ObservableCollection<HoldingDto> Holdings { get; } = new();

    public ObservableCollection<HoldingDisplayVm> HoldingRows { get; } = new();

    public ObservableCollection<HoldingDisplayVm> VisibleHoldings { get; } = new();

    public ObservableCollection<HoldingDisplayVm> OtherHoldings { get; } = new();

    public ObservableCollection<LocalWalletRow> LocalWallets { get; } = new();

    public ObservableCollection<AssetRowVm> CombinedAssets { get; } = new();

    public ObservableCollection<ChartPoint> Sparkline { get; } = new();

    public ObservableCollection<ChartSeries> CompareSeries { get; } = new();

    public ObservableCollection<string> CompareLegend { get; } = new();

    [ObservableProperty]
    private string totalUsd = "—";

    [ObservableProperty]
    private string change24H = "—";

    [ObservableProperty]
    private string coraLine = string.Empty;

    [ObservableProperty]
    private bool coraEnabled = true;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isStale;

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private string selectedRange = "1m";

    [ObservableProperty]
    private bool balancesHidden;

    public string HideToggleLabel => BalancesHidden ? "Show" : "Hide";

    partial void OnBalancesHiddenChanged(bool value) => OnPropertyChanged(nameof(HideToggleLabel));

    [ObservableProperty]
    private bool isOtherHoldingsExpanded;

    [ObservableProperty]
    private int otherHoldingsCount;

    public string OtherAssetsLabel => $"Other assets ({OtherHoldingsCount})";

    public bool HasOtherHoldings => OtherHoldingsCount > 0;

    partial void OnOtherHoldingsCountChanged(int value)
    {
        OnPropertyChanged(nameof(OtherAssetsLabel));
        OnPropertyChanged(nameof(HasOtherHoldings));
        if (value == 0)
        {
            IsOtherHoldingsExpanded = false;
        }
    }

    [ObservableProperty]
    private bool showCora = true;

    [ObservableProperty]
    private bool showBalance = true;

    [ObservableProperty]
    private bool showQuickActions = true;

    [ObservableProperty]
    private bool showPerformance = true;

    [ObservableProperty]
    private bool showHoldings = true;

    [ObservableProperty]
    private bool showLocalWallets = true;

    [ObservableProperty]
    private bool showCombinedAssets;

    [ObservableProperty]
    private int coraRow;

    [ObservableProperty]
    private int balanceRow = 1;

    [ObservableProperty]
    private int quickActionsRow = 2;

    [ObservableProperty]
    private int performanceRow = 3;

    [ObservableProperty]
    private int holdingsRow = 4;

    [ObservableProperty]
    private int localWalletsRow = 5;

    [ObservableProperty]
    private int combinedAssetsRow = 4;

    [RelayCommand]
    private async Task AppearingAsync()
    {
        EnsureStreamHooked();
        _session.Touch();
        await RefreshPortfolioAsync(soft: Holdings.Count > 0 || !string.Equals(TotalUsd, "—", StringComparison.Ordinal));
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
        if (!e.Type.Equals("RATE.TICK", StringComparison.OrdinalIgnoreCase)
            && !e.Type.Equals("balance.update", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = _refreshDebounce.DebounceAsync(async () =>
        {
            if (MainThread.IsMainThread)
            {
                await RefreshPortfolioAsync(soft: true);
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() => RefreshPortfolioAsync(soft: true));
            }
        });
    }

    private async Task RefreshPortfolioAsync(bool soft)
    {
        IsStale = soft;
        IsBusy = !soft;
        UserPrefs? prefs = null;
        try
        {
            prefs = await _prefs.LoadAsync();
            ApplySectionPrefs(prefs);
            if (!soft)
            {
                BalancesHidden = prefs.ValuesHiddenOnLaunch;
            }

            LocalWallets.Clear();
            foreach (var w in await _wallets.ListAsync())
            {
                LocalWallets.Add(w);
            }

            var portfolio = await _api.GetPortfolioAsync();
            _lastPortfolioOk = true;
            _rawTotalUsd = "$" + portfolio.TotalUsd;
            _rawChange24H = portfolio.Change24HPct + "%";
            ApplyBalanceMask();

            Holdings.Clear();
            foreach (var h in portfolio.Holdings)
            {
                Holdings.Add(h);
            }

            _enabledCurrencies = prefs.EnabledCurrencies;
            RefreshHoldingRows();

            RebuildCombinedAssets();
            await ReloadChartsAsync();
        }
        finally
        {
            if (prefs is not null)
            {
                EnqueueRatesHydrate(prefs);
            }

            RefreshOnline();
            IsBusy = false;
            IsStale = false;
        }
    }

    private void RefreshOnline()
        => IsOnline = _stream.IsConnected || _lastPortfolioOk;

    [RelayCommand]
    private void ToggleBalancesHidden()
    {
        BalancesHidden = !BalancesHidden;
        ApplyBalanceMask();
        RefreshHoldingRows();
        RebuildCombinedAssets();
    }

    [RelayCommand]
    private async Task SetRangeAsync(string range)
    {
        SelectedRange = range;
        IsStale = true;
        try
        {
            await ReloadChartsAsync();
        }
        finally
        {
            IsStale = false;
        }
    }

    private void ApplySectionPrefs(UserPrefs prefs)
    {
        CoraEnabled = prefs.CoraEnabled;
        bool IsVis(string key) => prefs.HomeVisible.TryGetValue(key, out bool v) && v;
        bool combined = prefs.AssetsLayout.Equals("combined", StringComparison.OrdinalIgnoreCase);

        // Brand header follows home-order visibility; CoraEnabled only gates the FAB.
        ShowCora = IsVis("cora");
        ShowBalance = IsVis("balance");
        ShowQuickActions = IsVis("quickActions");
        ShowPerformance = IsVis("performance");
        ShowHoldings = !combined && IsVis("holdings");
        ShowLocalWallets = !combined && IsVis("localWallets");
        ShowCombinedAssets = combined && (IsVis("holdings") || IsVis("localWallets"));

        var slots = new List<(string Key, bool Visible, Action<int> SetRow)>();
        bool combinedPlaced = false;
        foreach (string key in prefs.HomeOrder)
        {
            if (key is "holdings" or "localWallets" or "assets")
            {
                if (combined)
                {
                    if (!combinedPlaced)
                    {
                        slots.Add(("combined", ShowCombinedAssets, r => CombinedAssetsRow = r));
                        combinedPlaced = true;
                    }

                    continue;
                }

                if (key == "holdings")
                {
                    slots.Add(("holdings", ShowHoldings, r => HoldingsRow = r));
                }
                else if (key == "localWallets")
                {
                    slots.Add(("localWallets", ShowLocalWallets, r => LocalWalletsRow = r));
                }

                continue;
            }

            switch (key)
            {
                case "cora":
                    slots.Add((key, ShowCora, r => CoraRow = r));
                    break;
                case "balance":
                    slots.Add((key, ShowBalance, r => BalanceRow = r));
                    break;
                case "quickActions":
                    slots.Add((key, ShowQuickActions, r => QuickActionsRow = r));
                    break;
                case "performance":
                    slots.Add((key, ShowPerformance, r => PerformanceRow = r));
                    break;
            }
        }

        if (!combined)
        {
            if (slots.All(s => s.Key != "holdings"))
            {
                slots.Add(("holdings", ShowHoldings, r => HoldingsRow = r));
            }

            if (slots.All(s => s.Key != "localWallets"))
            {
                slots.Add(("localWallets", ShowLocalWallets, r => LocalWalletsRow = r));
            }

            CombinedAssetsRow = 20;
        }
        else
        {
            HoldingsRow = 20;
            LocalWalletsRow = 20;
            if (!combinedPlaced)
            {
                slots.Add(("combined", ShowCombinedAssets, r => CombinedAssetsRow = r));
            }
        }

        int row = 0;
        foreach (var slot in slots)
        {
            if (!slot.Visible)
            {
                slot.SetRow(20);
                continue;
            }

            slot.SetRow(row++);
        }
    }

    private void ApplyBalanceMask()
    {
        if (BalancesHidden)
        {
            TotalUsd = "••••";
            Change24H = "••••";
        }
        else
        {
            TotalUsd = _rawTotalUsd;
            Change24H = _rawChange24H;
        }
    }

    private void RefreshHoldingRows()
    {
        HoldingRows.Clear();
        VisibleHoldings.Clear();
        OtherHoldings.Clear();

        HoldingVisibilityResult partition = HoldingVisibility.Split(Holdings, _enabledCurrencies);
        foreach (var h in partition.Visible)
        {
            HoldingDisplayVm row = ToHoldingRow(h);
            HoldingRows.Add(row);
            VisibleHoldings.Add(row);
        }

        foreach (var h in partition.Other)
        {
            OtherHoldings.Add(ToHoldingRow(h));
        }

        OtherHoldingsCount = OtherHoldings.Count;
    }

    private void RebuildCombinedAssets()
    {
        CombinedAssets.Clear();
        foreach (var h in VisibleHoldings)
        {
            CombinedAssets.Add(new AssetRowVm
            {
                Symbol = h.Symbol,
                Detail = h.Balance,
                Trailing = h.UsdValue,
                Accent = HoldingsAccent,
                KindLabel = "holding",
            });
        }

        foreach (var w in LocalWallets)
        {
            CombinedAssets.Add(new AssetRowVm
            {
                Symbol = w.Symbol,
                Detail = w.Label,
                Trailing = w.Kind,
                Accent = LocalAccent,
                KindLabel = "local",
            });
        }
    }

    [RelayCommand]
    private void ToggleOtherHoldings()
    {
        if (OtherHoldingsCount > 0)
        {
            IsOtherHoldingsExpanded = !IsOtherHoldingsExpanded;
        }
    }

    private HoldingDisplayVm ToHoldingRow(HoldingDto holding) => new()
    {
        Symbol = holding.Symbol,
        Balance = BalancesHidden ? "••••" : holding.Balance,
        UsdValue = BalancesHidden ? "••••" : holding.UsdValue,
    };

    private async Task ReloadChartsAsync()
    {
        Sparkline.Clear();
        CompareSeries.Clear();
        CompareLegend.Clear();

        var prefs = await _prefs.LoadAsync();
        string[] symbols = BuildChartSymbols(prefs);

        for (int i = 0; i < symbols.Length; i++)
        {
            var pts = await _api.GetHistoryAsync(symbols[i], SelectedRange);
            (long T, double V)[] ohlc = pts.Select(point => (point.T, point.V)).ToArray();
            string symbol = symbols[i];
            _syncJobQueue.Enqueue(
                $"p1-ohlc-{symbol.ToUpperInvariant()}",
                SyncPriority.P1,
                ct => _marketRepository.UpsertOhlcAsync(symbol, ohlc, ct));
            var chartPts = pts.Select(p => new ChartPoint(p.T, p.V)).ToList();
            if (i == 0)
            {
                foreach (var p in chartPts)
                {
                    Sparkline.Add(p);
                }
            }

            CompareSeries.Add(new ChartSeries
            {
                Label = symbols[i],
                Points = chartPts,
                Stroke = SeriesColors[i % SeriesColors.Length],
            });
            CompareLegend.Add(symbols[i]);
        }
    }

    private void EnqueueRatesHydrate(UserPrefs prefs)
    {
        var enabled = prefs.EnabledCurrencies.Count > 0
            ? prefs.EnabledCurrencies
            : UserPrefs.DefaultEnabledCurrencies.ToList();
        string[] heldEnabledSymbols = Holdings.Select(holding => holding.Symbol)
            .Concat(LocalWallets.Select(wallet => wallet.Symbol))
            .Where(symbol => enabled.Contains(symbol, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _syncJobQueue.Enqueue(
            "p2-rates",
            SyncPriority.P2,
            ct => MarketBootstrap.HydrateAndRefreshAsync(
                _ratesCache,
                _publicQuotes,
                heldEnabledSymbols,
                ct));
    }

    /// <summary>
    /// Chart symbols follow held assets ∩ enabled currencies (Expo Home behavior),
    /// falling back to enabled list then BTC/USD.
    /// </summary>
    private string[] BuildChartSymbols(UserPrefs prefs)
    {
        var enabled = prefs.EnabledCurrencies.Count > 0
            ? prefs.EnabledCurrencies
            : UserPrefs.DefaultEnabledCurrencies.ToList();

        var held = Holdings.Select(h => h.Symbol)
            .Concat(LocalWallets.Select(w => w.Symbol))
            .Where(s => enabled.Contains(s, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        if (held.Count == 0)
        {
            held = enabled.Take(3).ToList();
        }

        if (held.Count == 0)
        {
            held = ["BTC", "USD"];
        }

        return held.ToArray();
    }

    [RelayCommand]
    private Task GoConvertAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.Convert);
    }

    [RelayCommand]
    private Task GoSendAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.Send);
    }

    [RelayCommand]
    private Task GoReceiveAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.Receive);
    }

    [RelayCommand]
    private Task GoPayAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.Pay);
    }

    [RelayCommand]
    private Task AddWalletAsync()
    {
        _session.Touch();
        return _nav.GoToAsync(Routes.AddWallet);
    }
}

/// <summary>Unified asset row for combined holdings+local layout.</summary>
public sealed class AssetRowVm
{
    public string Symbol { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Trailing { get; set; } = string.Empty;

    public Color Accent { get; set; } = Colors.Gray;

    public string KindLabel { get; set; } = string.Empty;
}

/// <summary>Holdings row with optional masked balances.</summary>
public sealed class HoldingDisplayVm
{
    public string Symbol { get; set; } = string.Empty;

    public string Balance { get; set; } = string.Empty;

    public string UsdValue { get; set; } = string.Empty;
}
