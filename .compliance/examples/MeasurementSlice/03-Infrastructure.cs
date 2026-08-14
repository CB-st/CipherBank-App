using System.Net.Http.Json;
using System.Diagnostics;
using Example.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Example.Infrastructure;

public sealed class CatalogClient(HttpClient client) : ISpeciesCatalog
{
    public async Task<bool> ExistsAsync(string species, CancellationToken cancellationToken)
    {
        var result = await client.GetFromJsonAsync<string[]>(
            $"species?name={Uri.EscapeDataString(species)}",
            cancellationToken);
        return result is { Length: > 0 };
    }
}

public sealed class EfMeasurementStore(AppDbContext database) : IMeasurementStore
{
    public async Task UpsertAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var entity = await database.Measurements.FindAsync([measurement.Id.Value], cancellationToken);
        if (entity is null)
        {
            database.Measurements.Add(MeasurementEntity.FromDomain(measurement));
        }
        else
        {
            entity.UpdateFrom(measurement);
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}

public sealed class LoggingMeasurementStore(
    IMeasurementStore inner,
    ILogger<LoggingMeasurementStore> logger) : IMeasurementStore
{
    public async Task UpsertAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        await inner.UpsertAsync(measurement, cancellationToken);
        logger.LogInformation(
            "Stored measurement {SampleId} for {Species} in {ElapsedMs} ms",
            measurement.Id.Value,
            measurement.Species,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
}
