using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

public partial class WalletPage : ContentPage
{
    private readonly WalletViewModel _viewModel;

    public WalletPage(WalletViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
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
}
