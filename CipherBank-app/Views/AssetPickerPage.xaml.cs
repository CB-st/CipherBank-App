// <copyright file="AssetPickerPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>
/// Modal sheet listing all available assets; tapping one selects it on the Buy page.
/// Constructed with the Buy page's live ViewModel so selection flows straight back.
/// </summary>
public partial class AssetPickerPage : ContentPage
{
    private readonly PurchaseViewModel _viewModel;
    private bool _isClosing;

    public AssetPickerPage(PurchaseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnAssetSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_isClosing || e.CurrentSelection.Count == 0 || e.CurrentSelection[0] is not CryptoCurrency crypto)
        {
            return;
        }

        _isClosing = true;
        _viewModel.SelectedCrypto = crypto;
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        await Navigation.PopModalAsync();
    }
}
