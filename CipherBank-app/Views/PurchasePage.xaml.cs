using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

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
}
