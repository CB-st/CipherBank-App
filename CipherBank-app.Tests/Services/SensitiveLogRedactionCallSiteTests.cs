// <copyright file="SensitiveLogRedactionCallSiteTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Guards the MAUI call sites that must redact sensitive log values.
/// The app project is not referenced by these tests, so the regression is the source call.
/// </summary>
public sealed class SensitiveLogRedactionCallSiteTests
{
    [Fact]
    public void LoginViewModel_LogsRedactedUsername()
    {
        string source = ReadRepoFile("CipherBank-app/ViewModels/LoginViewModel.cs");

        source.Should().Contain("LogRedactionHelper.RedactUsername(Username)");
        source.Should().NotContain("LogAttemptingLogin(_logger, Username)");
    }

    [Fact]
    public void SettingsService_LogsRedactedApiBaseUrlAndDescribesPreferencesAccurately()
    {
        string source = ReadRepoFile("CipherBank-app/Services/SettingsService.cs");

        source.Should().Contain("LogRedactionHelper.Redact(value)");
        source.Should().NotContain("LogSettingChanged(_logger, \"CipherBankEndpointBase\", value)");
        source.Should().NotContain("secure preferences storage");
        source.Should().Contain("persisted with MAUI Preferences");
        source.Should().Contain("is not SecureStorage");
    }

    private static string ReadRepoFile(string relativePath)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string solution = Path.Combine(directory.FullName, "CipherBank-app.sln");
            if (File.Exists(solution))
            {
                return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate CipherBank-app.sln from the test output directory.");
    }
}
