// <copyright file="PurchasePage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;
using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Purchase page.
/// </summary>
public partial class PurchasePage
{
    private readonly PurchaseViewModel _viewModel;
    private bool _isOpeningPicker;

    public PurchasePage(
        PurchaseViewModel viewModel,
        IMotionPreference motionPreference)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        CoinDeck.ReduceMotion = motionPreference.IsReduceMotionEnabled;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAvailableCryptosCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Shell raises this when the asset picker is pushed and when the Buy tab
        // is left. Either event cancels a purchase the server may already have
        // accepted, and the view model logs that cancellation without a dialog.
        if (_viewModel.IsPurchasing || Navigation.ModalStack.Count > 0)
        {
            return;
        }

        _viewModel.OnDisappearing();
    }

    private async void OnViewAllClicked(object? sender, EventArgs e)
    {
        if (_isOpeningPicker)
        {
            return;
        }

        _isOpeningPicker = true;
        try
        {
            await Navigation.PushModalAsync(new AssetPickerPage(_viewModel));
        }
        finally
        {
            _isOpeningPicker = false;
        }
    }
}
