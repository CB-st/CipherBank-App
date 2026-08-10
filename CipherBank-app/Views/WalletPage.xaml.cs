// <copyright file="WalletPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.Specialized;
using System.ComponentModel;
using CipherBank_app.Controls;
using CipherBank_app.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace CipherBank_app.Views;

/// <summary>
/// Code-behind for the Wallet page: triggers the initial load and manages the deck's
/// page-indicator dots, the detail-panel cross-fade, and the empty state.
/// </summary>
public partial class WalletPage : ContentPage
{
    private readonly WalletViewModel _viewModel;
    private bool _isCrossFading;

    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.WalletCards.CollectionChanged += OnWalletCardsChanged;
        WalletDeck.PropertyChanged += OnDeckPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadWalletsCommand.ExecuteAsync(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private static Color GetColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Gray;

    private void OnWalletCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildIndicator();
        UpdateEmptyState();
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WalletViewModel.SelectedWallet):
                await CrossFadeDetailPanelAsync();
                break;

            case nameof(WalletViewModel.FocusedWalletCard):
                UpdateIndicatorHighlight();
                break;

            case nameof(WalletViewModel.IsLoading):
                UpdateEmptyState();
                break;
        }
    }

    private void OnDeckPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArcCardDeck.DragFraction))
        {
            DetailPanel.Opacity = 1.0 - (0.5 * WalletDeck.DragFraction);
        }
    }

    private void RebuildIndicator()
    {
        PageIndicator.Children.Clear();

        for (int i = 0; i < _viewModel.WalletCards.Count; i++)
        {
            Border dot = new Border
            {
                WidthRequest = 8,
                HeightRequest = 8,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Opacity = 0.35,
            };
            dot.SetAppThemeColor(BackgroundColorProperty, GetColor("Accent"), GetColor("AccentDark"));
            PageIndicator.Children.Add(dot);
        }

        UpdateIndicatorHighlight();
    }

    private void UpdateIndicatorHighlight()
    {
        int focusedIndex = _viewModel.FocusedWalletCard != null
            ? _viewModel.WalletCards.IndexOf(_viewModel.FocusedWalletCard)
            : -1;

        for (int i = 0; i < PageIndicator.Children.Count; i++)
        {
            if (PageIndicator.Children[i] is View dot)
            {
                bool active = i == focusedIndex;
                dot.Opacity = active ? 1.0 : 0.35;
                dot.Scale = active ? 1.3 : 1.0;
            }
        }
    }

    private void UpdateEmptyState() =>
        EmptyWalletsLabel.IsVisible = _viewModel.WalletCards.Count == 0 && !_viewModel.IsLoading;

    private async Task CrossFadeDetailPanelAsync()
    {
        if (_isCrossFading)
        {
            return;
        }

        _isCrossFading = true;
        try
        {
            await DetailPanel.FadeToAsync(0.0, 90, Easing.CubicOut);
            await DetailPanel.FadeToAsync(1.0, 180, Easing.CubicIn);
        }
        catch (Exception)
        {
            // Animation failure must never crash or leave the panel hidden.
            DetailPanel.Opacity = 1.0;
        }
        finally
        {
            _isCrossFading = false;
        }
    }
}
