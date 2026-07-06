// <copyright file="PurchasePage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Purchase page.
/// </summary>
public partial class PurchasePage : ContentPage
{
    private readonly PurchaseViewModel _viewModel;

    public PurchasePage(PurchaseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAvailableCryptosCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private async void OnViewAllClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(new AssetPickerPage(_viewModel));
}
