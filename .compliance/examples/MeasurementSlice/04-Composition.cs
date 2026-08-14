using Example.Application;
using Example.Core;
using Example.Infrastructure;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ImportMeasurementHandler>();
builder.Services.AddScoped<IMeasurementStore, EfMeasurementStore>();
builder.Services.Decorate<IMeasurementStore, LoggingMeasurementStore>();
builder.Services.AddOptions<CatalogOptions>()
    .BindConfiguration("Catalog")
    .Validate(options => options.BaseAddress.IsAbsoluteUri, "Catalog address must be absolute.")
    .ValidateOnStart();
builder.Services.AddHttpClient<ISpeciesCatalog, CatalogClient>((services, client) =>
    client.BaseAddress = services.GetRequiredService<IOptions<CatalogOptions>>().Value.BaseAddress)
    .AddStandardResilienceHandler();

var app = builder.Build();
app.MapPost("/measurements", async (
    MeasurementRequest request,
    ImportMeasurementHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new ImportMeasurement(new SampleId(request.Id), request.Species, request.Value),
        cancellationToken);
    return result switch
    {
        ImportAccepted accepted => Results.Ok(accepted.Measurement),
        ImportRejected rejection => Results.ValidationProblem(
            new Dictionary<string, string[]> { ["measurement"] = [rejection.Code] }),
        _ => Results.Problem()
    };
});
await app.RunAsync();

public sealed class CatalogOptions
{
    public required Uri BaseAddress { get; init; }
}

public sealed record MeasurementRequest(Guid Id, string Species, double Value);
