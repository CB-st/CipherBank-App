// <copyright file="VerifySonarQualityGateTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Architecture;

public sealed class VerifySonarQualityGateTests
{
    private const string ThreeConditionGateJson =
        """
        {
          "status": "ERROR",
          "conditions": [
            {
              "status": "ERROR",
              "metricKey": "new_coverage",
              "comparator": "LT",
              "errorThreshold": "80",
              "actualValue": "0.0"
            },
            {
              "status": "OK",
              "metricKey": "new_duplicated_lines_density",
              "comparator": "GT",
              "errorThreshold": "3",
              "actualValue": "0.0"
            },
            {
              "status": "ERROR",
              "metricKey": "new_violations",
              "comparator": "GT",
              "errorThreshold": "0",
              "actualValue": "7"
            }
          ]
        }
        """;

    private const string ExtraDeclaredConditionYaml =
        """
        conditions:
          coverage:
            operator: less_than
            error_threshold: 80
          duplicated_lines_density:
            operator: greater_than
            error_threshold: 3
          violations:
            operator: greater_than
            error_threshold: 0
          reliability_rating:
            operator: greater_than
            error_threshold: 1
        """;

    [Fact]
    public void Verify_FailsWhenDeclaredYamlConditionsAreMissingFromFetchedGate()
    {
        (int exitCode, string stdErr) = RunVerifier(ThreeConditionGateJson, ExtraDeclaredConditionYaml);

        exitCode.Should().Be(1);
        stdErr.Should().Contain("reliability_rating");
        stdErr.Should().Contain("no matching fetched Sonar metric");
    }

    [Fact]
    public void Verify_SucceedsWhenEveryDeclaredConditionIsPresent()
    {
        (int exitCode, string stdErr) = RunVerifier(ThreeConditionGateJson);

        exitCode.Should().Be(0);
        stdErr.Should().BeEmpty();
    }

    private static (int ExitCode, string StandardError) RunVerifier(string fetchedJson, string? yamlOverride = null)
    {
        string root = FindRepositoryRoot();
        string script = Path.Combine(root, "scripts", "verify-sonar-quality-gate.py");
        string work = Directory.CreateTempSubdirectory("cb-qg-").FullName;
        try
        {
            string jsonPath = Path.Combine(work, "quality-gate.json");
            File.WriteAllText(jsonPath, fetchedJson);
            string yamlPath = Path.Combine(root, "config", "sonar", "quality-gate.yaml");
            if (yamlOverride is not null)
            {
                yamlPath = Path.Combine(work, "quality-gate.yaml");
                File.WriteAllText(yamlPath, yamlOverride);
            }

            var start = new ProcessStartInfo
            {
                FileName = "python3",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(script);
            start.ArgumentList.Add(jsonPath);
            start.ArgumentList.Add(yamlPath);

            using var process = Process.Start(start);
            process.Should().NotBeNull();
            string stdErr = process!.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, stdErr);
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "scripts", "verify-sonar-quality-gate.py")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate scripts/verify-sonar-quality-gate.py from test output.");
    }
}
