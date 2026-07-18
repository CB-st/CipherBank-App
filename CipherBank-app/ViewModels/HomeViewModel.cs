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

/// <summary>Home portfolio shell — polished Phase C.</summary>
public partial class HomeViewModel : ObservableObject
{
    private static readonly Color[] SeriesColors =
    {
        Color.FromArgb("#2B59FF"),
        Color.FromArgb("#22C55E"),
        Color.FromArgb("#F59E0B"),
    };

    private readonly IProductApi _api;
    private readonly IPrefsStore _prefs;
    private readonly IWalletRepository _wallets;
    private readonly INavigationService _nav;
    private readonly IAppSession _session;

    public HomeViewModel(
        IProductApi api,
        IPrefsStore prefs,
        IWalletRepository wallets,
        INavigationService nav,
        IAppSession session)
    {
        _api = api;
        _prefs = prefs;
        _wallets = wallets;
        _nav = nav;
        _session = session;
        CoraLine = CoraLines.For("home");
    }

    public ObservableCollection<HoldingDto> Holdings { get; } = new();

    public ObservableCollection<LocalWalletRow> LocalWallets { get; } = new();

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
    private string selectedRange = "30d";

    [RelayCommand]
    private async Task AppearingAsync()
    {
        _session.Touch();
        IsBusy = true;
        try
        {
            var prefs = await _prefs.LoadAsync();
            CoraEnabled = prefs.CoraEnabled;
            var portfolio = await _api.GetPortfolioAsync();
            TotalUsd = "$" + portfolio.TotalUsd;
            Change24H = portfolio.Change24HPct + "%";
            Holdings.Clear();
            foreach (var h in portfolio.Holdings)
            {
                Holdings.Add(h);
            }

            LocalWallets.Clear();
            foreach (var w in await _wallets.ListAsync())
            {
                LocalWallets.Add(w);
            }

            await ReloadChartsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetRangeAsync(string range)
    {
        SelectedRange = range;
        await ReloadChartsAsync();
    }

    private async Task ReloadChartsAsync()
    {
        Sparkline.Clear();
        CompareSeries.Clear();
        CompareLegend.Clear();

        string[] symbols = { "BTC", "ETH", "USD" };
        for (int i = 0; i < symbols.Length; i++)
        {
            var pts = await _api.GetHistoryAsync(symbols[i], SelectedRange);
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
