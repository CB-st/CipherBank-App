// <copyright file="QualityGateYamlTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Architecture;

public sealed class QualityGateYamlTests
{
    [Fact]
    public void CheckedInGate_DeclaresNewCodeConditionsTheWorkflowCanVerify()
    {
        string yaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "config", "sonar", "quality-gate.yaml"));

        yaml.Should().Contain("scope: new_code");
        yaml.Should().Contain("coverage:");
        yaml.Should().Contain("error_threshold: 80");
        yaml.Should().Contain("duplicated_lines_density:");
        yaml.Should().Contain("violations:");
        yaml.Should().Contain("error_threshold: 0");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "config", "sonar", "quality-gate.yaml")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate config/sonar/quality-gate.yaml from test output.");
    }
}
