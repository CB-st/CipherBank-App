using Example.Core;

namespace Example.Application;

public sealed record ImportMeasurement(SampleId Id, string Species, double Value);

public sealed class ImportMeasurementHandler(
    ISpeciesCatalog catalog,
    IMeasurementStore store,
    TimeProvider timeProvider)
{
    public async Task<ImportResult> HandleAsync(
        ImportMeasurement command,
        CancellationToken cancellationToken)
    {
        if (!await catalog.ExistsAsync(command.Species, cancellationToken))
        {
            return new ImportRejected("species.unknown");
        }

        var result = Measurement.Create(
            command.Id,
            command.Species,
            command.Value,
            timeProvider.GetUtcNow());
        if (result is not ImportAccepted accepted)
        {
            return result;
        }

        await store.UpsertAsync(accepted.Measurement, cancellationToken);
        return accepted;
    }
}
