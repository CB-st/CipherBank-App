namespace Example.Core;

public readonly record struct SampleId(Guid Value);

public sealed record Measurement(
    SampleId Id,
    string Species,
    double Value,
    DateTimeOffset CapturedAt)
{
    public static ImportResult Create(
        SampleId id,
        string species,
        double value,
        DateTimeOffset capturedAt) =>
        string.IsNullOrWhiteSpace(species) ? new ImportRejected("species.required") :
        value < 0 || !double.IsFinite(value) ? new ImportRejected("value.range") :
        new ImportAccepted(new Measurement(id, species, value, capturedAt));
}

public abstract record ImportResult;
public sealed record ImportAccepted(Measurement Measurement) : ImportResult;
public sealed record ImportRejected(string Code) : ImportResult;

public interface IMeasurementStore
{
    Task UpsertAsync(Measurement measurement, CancellationToken cancellationToken);
}

public interface ISpeciesCatalog
{
    Task<bool> ExistsAsync(string species, CancellationToken cancellationToken);
}
