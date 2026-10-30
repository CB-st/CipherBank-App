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

    [Theory]
    [InlineData("_ = {|CB1005:Microsoft.Maui.Controls.Application.Current|};")]
    [InlineData("_ = {|CB1005:Application?.Current|};")]
    [InlineData("_ = {|CB1005:global::Application.Current|};")]
    [InlineData("_ = {|CB1005:Task.Factory.StartNew|}(() => 1);")]
    [InlineData("_ = {|CB1005:Task.Factory?.StartNew(() => 1).ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:Task.Factory?.StartNew(() => 1)?.ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:Task.Factory?.StartNew(() => 1)!.ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:(Application).Current|};")]
    [InlineData("_ = {|CB1005:((Application))?.Current|};")]
    [InlineData("_ = {|CB1005:(Task.Factory).StartNew|}(() => 1);")]
    [InlineData("_ = {|CB1005:((Task).Factory)?.StartNew(() => 1)|};")]
    public async Task ReportsQualifiedAndConditionalPlatformGlobalsFromViewModelAsync(string statement)
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/QualifiedGlobalViewModel.cs", $$"""
                        class QualifiedGlobalViewModel
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
    public async Task IgnoresTaskDelayFromViewModelAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/DelayViewModel.cs", """
                        class DelayViewModel
                        {
                            object Wait() => Task.Delay(1);
                            object WaitParenthesized() => ((Task)).Delay(1);
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

    [Theory]
    [InlineData("{|CB1005:Application.Current|}.UserAppTheme = 1;")]
    [InlineData("_ = {|CB1005:(Application).Current|};")]
    [InlineData("_ = {|CB1005:((Application))?.Current|};")]
    [InlineData("_ = {|CB1005:(Microsoft.Maui.Controls.Application).Current|};")]
    [InlineData("_ = {|CB1005:(Task.Factory).StartNew|}(() => 1);")]
    [InlineData("_ = {|CB1005:((Task).Factory)?.StartNew(() => 1)|};")]
    [InlineData("_ = {|CB1005:((global::System.Threading.Tasks.Task).Factory).StartNew|}(() => 1);")]
    public async Task ReportsPlatformGlobalFromViewModelAdditionalFileAsync(string statement)
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
                    ("CipherBank-app/ViewModels/SettingsViewModel.cs", $$"""
                        class SettingsViewModel
                        {
                            void Apply()
                            {
                                {{statement}}
                                _ = ((Task)).Delay(1);
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

    [Fact]
    public async Task IgnoresInstanceMembersFieldsLocalsParametersAndInjectedPortsAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/AccountViewModel.cs", """
                        interface ISettingsPort
                        {
                            PreferenceBag Preferences { get; }
                        }

                        class AccountProfile
                        {
                            public string Name { get; set; } = "";
                        }

                        class PreferenceBag
                        {
                            public string Get(string key, string fallback) => fallback;
                        }

                        class Account
                        {
                            public AccountProfile Application { get; } = new();
                        }

                        class AccountViewModel
                        {
                            private readonly Account _account = new();
                            private readonly ISettingsPort _settings;
                            private readonly PreferenceBag Preferences = new();
                            private readonly AccountProfile Application = new();

                            public AccountViewModel(ISettingsPort settings) => _settings = settings;

                            void Read(AccountProfile Application, PreferenceBag Preferences)
                            {
                                _ = _account.Application.Name;
                                _ = _settings.Preferences.Get("theme", "system");
                                _ = this.Application.Name;
                                _ = this.Preferences.Get("theme", "system");
                                _ = Application.Name;
                                _ = Preferences.Get("theme", "system");
                                _ = (Application).Name;
                                _ = (Preferences).Get("theme", "system");
                            }

                            void ReadLocal()
                            {
                                AccountProfile Application = _account.Application;
                                PreferenceBag Preferences = _settings.Preferences;
                                _ = Application.Name;
                                _ = Preferences.Get("theme", "system");
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresDomainTypeNamedLikePlatformGlobalAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/DomainApplicationViewModel.cs", """
                        using Bank.Domain;

                        class DomainApplicationViewModel
                        {
                            void Read()
                            {
                                _ = Application.Current;
                                _ = Application.Name;
                            }
                        }
                        """),
                    ("Domain/LoanApplication.cs", """
                        namespace Bank.Domain
                        {
                            public class Application
                            {
                                public static Application Current { get { return null; } }
                                public string Name { get { return "loan"; } }
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresAliasedTaskDelayFromViewModelAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/AliasedDelayViewModel.cs", """
                        using Work = System.Threading.Tasks.Task;

                        class AliasedDelayViewModel
                        {
                            object Wait() => Work.Delay(1);
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Theory]
    [InlineData("_ = {|CB1005:Application.Current|};")]
    [InlineData("_ = {|CB1005:Controls.Application.Current|};")]
    [InlineData("_ = {|CB1005:Microsoft.Maui.Controls.Application.Current|};")]
    [InlineData("_ = {|CB1005:global::Microsoft.Maui.Controls.Application.Current|};")]
    [InlineData("_ = {|CB1005:MauiApplication.Current|};")]
    [InlineData("_ = {|CB1005:Application?.Current|};")]
    [InlineData("_ = {|CB1005:Preferences.Get|}(\"theme\", \"system\");")]
    [InlineData("_ = {|CB1005:Storage.Preferences.Get|}(\"theme\", \"system\");")]
    [InlineData("_ = {|CB1005:StoredPreferences.Get|}(\"theme\", \"system\");")]
    [InlineData("_ = {|CB1005:Work.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Work.Factory.StartNew|}(() => 1);")]
    [InlineData("_ = {|CB1005:Work.Factory?.StartNew(() => 1).ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:Work.Factory?.StartNew(() => 1)?.ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:Work.Factory?.StartNew(() => 1)!.ContinueWith(t => t)|};")]
    [InlineData("_ = {|CB1005:(Application).Current|};")]
    [InlineData("_ = {|CB1005:((MauiApplication)).Current|};")]
    [InlineData("_ = {|CB1005:(Work.Factory).StartNew|}(() => 1);")]
    [InlineData("_ = {|CB1005:((Work).Factory)?.StartNew(() => 1)|};")]
    public async Task ReportsSymbolResolvedPlatformGlobalsFromViewModelAsync(string statement)
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/ResolvedGlobalViewModel.cs", $$"""
                        using Microsoft.Maui;
                        using Microsoft.Maui.Controls;
                        using Microsoft.Maui.Storage;
                        using MauiApplication = Microsoft.Maui.Controls.Application;
                        using StoredPreferences = Microsoft.Maui.Storage.Preferences;
                        using Work = System.Threading.Tasks.Task;

                        class ResolvedGlobalViewModel
                        {
                            void Execute()
                            {
                                {{statement}}
                            }
                        }
                        """),
                    ("Stubs/MauiPlatformGlobals.cs", """
                        namespace Microsoft.Maui.Controls
                        {
                            public class Application
                            {
                                public static Application Current { get { return null; } }
                                public int UserAppTheme { get; set; }
                            }
                        }

                        namespace Microsoft.Maui.Storage
                        {
                            public static class Preferences
                            {
                                public static string Get(string key, string fallback) => fallback;
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresInstanceMemberChainsInAdditionalViewModelFileAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Anchor { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app/ViewModels/DomainGlobalNamesViewModel.cs", """
                        class DomainGlobalNamesViewModel
                        {
                            void Read()
                            {
                                _ = _account.Application.Name;
                                _ = _settings.Preferences.Get("theme", "system");
                                _ = settings.Preferences.Get("theme", "system");
                                _ = account.Application.Name;
                                _ = Controls.Application.Current;
                                _ = Storage.Preferences.Get("theme", "system");
                                {|CB1005:Application.Current|}.UserAppTheme = 1;
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Theory]
    [InlineData("_ = {|CB1005:Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:System.Threading.Tasks.Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:global::Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:global::System.Threading.Tasks.Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Tasks.Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Threading.Tasks.Task<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Work<int>.Run|}(() => 1);")]
    [InlineData("_ = {|CB1005:Work<int>.Factory.StartNew|}(() => 1);")]
    public async Task ReportsGenericTaskReceiversWhenSymbolResolvesAsync(string statement)
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/GenericTaskViewModel.cs", $$"""
                        using System;
                        using System.Threading;
                        using Work = System.Threading.Tasks.Task;

                        class GenericTaskViewModel
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
    public async Task IgnoresNonPlatformGenericReceiversAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/ViewModels/LocalGenericViewModel.cs", """
                        using Work = System.Threading.Tasks.Task;

                        class Task<T>
                        {
                            public static int Run(System.Func<T> action) => 0;
                        }

                        class Widget<T>
                        {
                            public static Widget<T> Current { get { return null; } }
                        }

                        class LocalGenericViewModel
                        {
                            void Execute()
                            {
                                _ = Task<int>.Run(() => 1);
                                _ = Widget<int>.Current;
                                _ = Work<int>.Delay(1);
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsGenericTaskReceiversInAdditionalViewModelFileAsync()
    {
        CSharpAnalyzerTest<NoViewModelPlatformGlobalsAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Anchor { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app/ViewModels/GenericTaskAdditionalViewModel.cs", """
                        class GenericTaskAdditionalViewModel
                        {
                            void Execute()
                            {
                                _ = {|CB1005:Task<int>.Run|}(() => 1);
                                _ = {|CB1005:System.Threading.Tasks.Task<int>.Run|}(() => 1);
                                _ = {|CB1005:global::Task<int>.Run|}(() => 1);
                                _ = {|CB1005:global::System.Threading.Tasks.Task<int>.Run|}(() => 1);
                                _ = Tasks.Task<int>.Run(() => 1);
                                _ = Threading.Tasks.Task<int>.Run(() => 1);
                                _ = {|CB1005:Task<int>.Factory.StartNew|}(() => 1);
                                _ = _scheduler.Task<int>.Run(() => 1);
                                _ = Widget<int>.Current;
                                _ = Task<int>.Delay(1);
                            }
                        }
                        """),
                },
            },
        };

        await test.RunAsync();
    }
}
