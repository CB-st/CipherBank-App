using Example.Application;
using Example.Core;
using Example.UI;
using NSubstitute;

namespace Example.Tests;

public sealed class MeasurementImportViewModelTests
{
    [Fact]
    public async Task ImportCommand_SetsErrorMessage_ForUnknownSpecies()
    {
        var catalog = Substitute.For<ISpeciesCatalog>();
        var store = Substitute.For<IMeasurementStore>();
        catalog.ExistsAsync("Xx", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ImportMeasurementHandler(catalog, store, TimeProvider.System);
        var viewModel = new MeasurementImportViewModel(handler)
        {
            Species = "Xx",
            Value = 1
        };

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
        Assert.Equal("species.unknown", viewModel.ErrorMessage);
        await store.DidNotReceive().UpsertAsync(Arg.Any<Measurement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportCommand_ClearsErrorMessage_ForAcceptedMeasurement()
    {
        var catalog = Substitute.For<ISpeciesCatalog>();
        var store = Substitute.For<IMeasurementStore>();
        catalog.ExistsAsync("Np", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new ImportMeasurementHandler(catalog, store, TimeProvider.System);
        var viewModel = new MeasurementImportViewModel(handler)
        {
            Species = "Np",
            Value = 1.2
        };

        await viewModel.ImportCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsBusy);
        Assert.Null(viewModel.ErrorMessage);
        await store.Received(1).UpsertAsync(Arg.Any<Measurement>(), Arg.Any<CancellationToken>());
    }
}
