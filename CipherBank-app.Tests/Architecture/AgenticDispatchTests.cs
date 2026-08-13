// <copyright file="AgenticDispatchTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Architecture;

public sealed class AgenticDispatchTests
{
    private static readonly string[] RequiredWorkflowIds =
    [
        "feature-slice",
        "core-service",
        "ui-flow",
        "persistence",
        "device-journey",
        "validation",
    ];

    [Fact]
    public void DispatchMap_ReferencesExistingContractsTemplatesAndGates()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "config", "agentic", "dispatch.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        JsonElement workflows = document.RootElement.GetProperty("workflows");
        string[] ids = workflows.EnumerateArray()
            .Select(workflow => workflow.GetProperty("id").GetString())
            .OfType<string>()
            .ToArray();
        ids.Should().Contain(RequiredWorkflowIds);
        ids.Should().OnlyHaveUniqueItems();

        foreach (JsonElement workflow in workflows.EnumerateArray())
        {
            string id = workflow.GetProperty("id").GetString() ?? "<unnamed>";
            string? skill = workflow.GetProperty("skill").GetString();
            skill.Should().StartWith("cipherbank-", $"{id} must select a CipherBank skill");
            AssertPathsExist(root, id, workflow.GetProperty("templates"));
            AssertPathsExist(root, id, workflow.GetProperty("references"));
            workflow.GetProperty("gates").GetArrayLength()
                .Should().BeGreaterThan(0, $"{id} must define verification");
        }
    }

    private static void AssertPathsExist(string root, string workflowId, JsonElement paths)
    {
        foreach (JsonElement item in paths.EnumerateArray())
        {
            string relativePath = item.GetString() ?? string.Empty;
            File.Exists(Path.Combine(root, relativePath))
                .Should().BeTrue($"{workflowId} references {relativePath}");
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CipherBank-app.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CipherBank-app.sln from test output.");
    }
}
