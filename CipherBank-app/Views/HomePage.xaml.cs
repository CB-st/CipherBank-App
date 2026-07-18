// <copyright file="HomePage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Controls;
using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Home portfolio page.</summary>
public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm;
    private readonly SparklineDrawable _spark = new();
    private readonly CompareChartDrawable _compare = new();

    public HomePage(HomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        SparkView.Drawable = _spark;
        CompareView.Drawable = _compare;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.AppearingCommand.ExecuteAsync(null);
        RefreshCharts();
    }

    private void RefreshCharts()
    {
        _spark.Series = _vm.Sparkline.ToList();
        SparkView.Invalidate();
        _compare.Series = _vm.CompareSeries.ToList();
        CompareView.Invalidate();
    }
}
