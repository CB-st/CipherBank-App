// <copyright file="ConfigSeedPayeeIdTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Stories that list payees must use Persistence:DefaultRecipients ids, not generated GUIDs.
/// Use: High (US-SND-01 / package reset). Scope: E2E seed contract.
/// </summary>
public sealed class ConfigSeedPayeeIdTests
{
    [Fact]
    public void PersistenceDefaultRecipients_UseStableConfigIdsNotGuids()
    {
        string json = File.ReadAllText(FindAppSettings());
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement rows = document.RootElement.GetProperty("Persistence").GetProperty("DefaultRecipients");
        string[] ids = rows.EnumerateArray()
            .Select(row => row.GetProperty("Id").GetString() ?? string.Empty)
            .ToArray();
        ids.Should().Equal("seed:rent-4th-st", "seed:utilities-co");
        foreach (string id in ids)
        {
            Guid.TryParse(id, out _).Should().BeFalse();
        }
    }

    private static string FindAppSettings()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "config", "persistence", "database.json");
            if (File.Exists(candidate) && File.Exists(Path.Combine(directory.FullName, "CipherBank-app.sln")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate config/persistence/database.json from the E2E test assembly.");
    }
}
