// <copyright file="NoViewModelPlatformGlobalsAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoViewModelPlatformGlobalsAnalyzerTests
{
    [Theory]
    [InlineData("_ = {|CB1005:Preferences.Get|}(\"theme\", \"system\");")]
    [InlineData("_ = {|CB1005:Shell.Current|}.GoToAsync(\"//home\");")]
    [InlineData("_ = {|CB1005:Task.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Application.Current|};")]
    [InlineData("{|CB1005:Application.Current|}.UserAppTheme = 1;")]
    [InlineData("_ = {|CB1005:MainThread.IsMainThread|};")]
    [InlineData("_ = {|CB1005:SecureStorage.Default|};")]
    public async Task ReportsPlatformGlobalFromViewModelAsync(string statement)
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/ProfileViewModel.cs", $$"""
                        class ProfileViewModel
                        {
                            void Execute()
                            {
                                {{statement}}
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresPlatformUseOutsideViewModelsAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/Services/PreferenceStore.cs", """
                        class PreferenceStore
                        {
                            object Read() => Preferences.Get("theme", "system");
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresCancellableTaskDelayFromViewModelAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/TimerViewModel.cs", """
                        class TimerViewModel
                        {
                            object Tick(System.Threading.CancellationToken token)
                                => Task.Delay(1000, token);
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsPlatformGlobalFromViewModelAdditionalFileAsync()
    {
        // CI structure builds compile only Core/Tests/Analyzers; the MAUI host
        // arrives as AdditionalFiles, so CB1005 must also scan that surface.
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Anchor { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app/ViewModels/SettingsViewModel.cs", """
                        class SettingsViewModel
                        {
                            void Apply()
                            {
                                {|CB1005:Application.Current|}.UserAppTheme = 1;
                            }
                        }
                        """),
                    ("CipherBank-app/Services/PreferenceStore.cs", """
                        class PreferenceStore
                        {
                            object Read() => Preferences.Get("theme", "system");
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotDoubleReportWhenAdditionalFileIsCompilationTreeAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/HomeViewModel.cs", """
                        class HomeViewModel
                        {
                            void Execute() => _ = {|CB1005:Task.Run|}(() => 1);
                        }
                        """),
                },
                AdditionalFiles =
                {
                    ("CipherBank-app/ViewModels/HomeViewModel.cs", """
                        class HomeViewModel
                        {
                            void Execute() => _ = Task.Run(() => 1);
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }
}
