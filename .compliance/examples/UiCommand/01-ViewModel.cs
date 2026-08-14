using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Example.Application;
using Example.Core;

namespace Example.UI;

public sealed partial class MeasurementImportViewModel(
    ImportMeasurementHandler handler) : ObservableObject
{
    [ObservableProperty]
    private string species = string.Empty;

    [ObservableProperty]
    private double value;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ImportAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await handler.HandleAsync(
                new ImportMeasurement(new SampleId(Guid.NewGuid()), Species, Value),
                cancellationToken);
            ErrorMessage = result is ImportRejected rejected ? rejected.Code : null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
