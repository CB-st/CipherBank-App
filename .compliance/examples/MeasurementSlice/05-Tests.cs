using Example.Application;
using Example.Core;
using NSubstitute;

namespace Example.Tests;

public sealed class ImportMeasurementHandlerTests
{
    [Fact]
    public async Task HandleAsync_StoresValidKnownSpecies()
    {
        var catalog = Substitute.For<ISpeciesCatalog>();
        var store = Substitute.For<IMeasurementStore>();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));
        catalog.ExistsAsync("Np", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new ImportMeasurementHandler(catalog, store, clock);
        var command = new ImportMeasurement(new SampleId(Guid.NewGuid()), "Np", 1.2);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        var accepted = Assert.IsType<ImportAccepted>(result);
        Assert.Equal(clock.GetUtcNow(), accepted.Measurement.CapturedAt);
        await store.Received(1).UpsertAsync(accepted.Measurement, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_DoesNotStoreUnknownSpecies()
    {
        var catalog = Substitute.For<ISpeciesCatalog>();
        var store = Substitute.For<IMeasurementStore>();
        catalog.ExistsAsync("Xx", Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ImportMeasurementHandler(catalog, store, TimeProvider.System);

        var result = await handler.HandleAsync(
            new ImportMeasurement(new SampleId(Guid.NewGuid()), "Xx", 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(new ImportRejected("species.unknown"), result);
        await store.DidNotReceive().UpsertAsync(Arg.Any<Measurement>(), Arg.Any<CancellationToken>());
    }
}

public sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
