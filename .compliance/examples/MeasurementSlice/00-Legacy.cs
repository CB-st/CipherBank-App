// Legacy example: several responsibilities and contracts are hidden in one method.
public sealed class MeasurementController(AppDbContext database, IConfiguration configuration)
{
    public async Task<bool> ImportAsync(MeasurementRequest request)
    {
        using var client = new HttpClient { BaseAddress = new Uri(configuration["CatalogUrl"]!) };
        var species = await client.GetFromJsonAsync<string[]>($"species?name={request.Species}");
        if (species is null || species.Length == 0 || request.Value < 0)
        {
            return false;
        }

        var entity = await database.Measurements.FindAsync(request.Id);
        if (entity is null)
        {
            database.Measurements.Add(new MeasurementEntity
            {
                Id = request.Id,
                Species = request.Species,
                Value = request.Value,
                CapturedAt = DateTime.UtcNow
            });
        }
        else
        {
            entity.Value = request.Value;
            entity.CapturedAt = DateTime.UtcNow;
        }

        await database.SaveChangesAsync();
        return true;
    }
}
