// <copyright file="ConfigSeedPayeeIdTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Development stories that list payees use stable overlay ids, not generated GUIDs.
/// Use: High (US-SND-01 / package reset). Scope: E2E seed contract.
/// </summary>
public sealed class ConfigSeedPayeeIdTests
{
    [Fact]
    public void PersistenceDefaultRecipients_UseStableConfigIdsNotGuids()
    {
        string json = File.ReadAllText(FindDevelopmentAppSettings());

        // Mirror the runtime configuration reader, which skips // comments in appsettings files.
        JsonDocumentOptions options = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        using JsonDocument document = JsonDocument.Parse(json, options);
        JsonElement rows = document.RootElement.GetProperty("PersistenceOptions").GetProperty("DefaultRecipients");
        string[] ids = rows.EnumerateArray()
            .Select(row => row.GetProperty("Id").GetString() ?? string.Empty)
            .ToArray();
        ids.Should().Equal("seed:rent-4th-st", "seed:utilities-co");
        foreach (string id in ids)
        {
            Guid.TryParse(id, out _).Should().BeFalse();
        }
    }

    private static string FindDevelopmentAppSettings()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "config", "appsettings.Development.jsonc");
            if (File.Exists(candidate) && File.Exists(Path.Combine(directory.FullName, "CipherBank-app.sln")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate config/appsettings.Development.jsonc from the E2E test assembly.");
    }
}
