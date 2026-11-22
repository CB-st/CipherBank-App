// <copyright file="NoNonNullableNullCheckAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoNonNullableNullCheckAnalyzerTests
{
    [Theory]
    [InlineData("{|CB1006:System.ArgumentNullException.ThrowIfNull(value)|};")]
    [InlineData("{|CB1006:System.ArgumentNullException.ThrowIfNull(paramName: \"value\", argument: value)|};")]
    [InlineData("_ = {|CB1006:value == null|};")]
    [InlineData("_ = {|CB1006:value != null|};")]
    [InlineData("_ = {|CB1006:null == value|};")]
    [InlineData("_ = {|CB1006:null != value|};")]
    [InlineData("_ = {|CB1006:(value) == (null)|};")]
    [InlineData("_ = {|CB1006:value is null|};")]
    [InlineData("_ = {|CB1006:value is (null)|};")]
    [InlineData("_ = {|CB1006:value is not null|};")]
    [InlineData("_ = {|CB1006:value is not (null)|};")]
    [InlineData("_ = {|CB1006:value is { }|};")]
    [InlineData("_ = {|CB1006:value is { } copy|} ? copy : \"\";")]
    [InlineData("_ = {|CB1006:value is not { }|};")]
    [InlineData("_ = {|CB1006:object.ReferenceEquals(value, null)|};")]
    [InlineData("_ = {|CB1006:ReferenceEquals(null, value)|};")]
    [InlineData("_ = {|CB1006:object.Equals(value, null)|};")]
    [InlineData("_ = {|CB1006:value ?? \"fallback\"|};")]
    [InlineData("{|CB1006:value ??= \"fallback\"|};")]
    [InlineData("_ = {|CB1006:value?.Length|};")]
    [InlineData("_ = {|CB1006:value.Trim() is not null|};")]
    [InlineData("_ = {|CB1006:\"literal\" is null|};")]
    public async Task ReportsPureNullChecksOnNonNullableReferenceTypeAsync(string statement)
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest($$"""
            #nullable enable
            class Sample
            {
                void Execute(string value)
                {
                    {{statement}}
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsExpectedActionableMessageWithTypeNameAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest("""
            #nullable enable
            class Sample
            {
                void Execute(string value)
                {
                    {|#0:System.ArgumentNullException.ThrowIfNull(value)|};
                }
            }
            """);

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(CipherBankDiagnostics.NonNullableNullCheck)
                .WithLocation(0)
                .WithArguments("string")
                .WithMessage("Null check on non-nullable type 'string'; make the type nullable or remove the check"));

        await test.RunAsync();
    }

    [Theory]
    [InlineData("int", "{|CB1006:System.ArgumentNullException.ThrowIfNull(value)|};")]
    [InlineData("int", "_ = {|CB1006:value == null|};")]
    [InlineData("System.Guid", "_ = {|CB1006:value is null|};")]
    [InlineData("System.Guid", "_ = {|CB1006:value is not null|};")]
    [InlineData("System.Guid", "_ = {|CB1006:object.ReferenceEquals(value, null)|};")]
    [InlineData("int[]", "_ = {|CB1006:value?[0]|};")]
    [InlineData("string[]", "_ = {|CB1006:value[0] is null|};")]
    [InlineData("string", "_ = {|CB1006:new Sample() is null|};")]
    [InlineData("string", "_ = {|CB1006:new Sample() == null|};")]
    public async Task ReportsNullChecksOnValueTypesAndArraysAsync(string parameterType, string statement)
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest(
            $$"""
            #nullable enable
            class Sample
            {
                void Execute({{parameterType}} value)
                {
                    {{statement}}
                }
            }
            """,
            CompilerDiagnostics.None);

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsStaticUsingThrowIfNullAndChainedNonNullableConditionalAccessAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest("""
            #nullable enable
            using static System.ArgumentNullException;

            sealed class Outer
            {
                public Inner Required { get; } = new();
                public Inner? Optional { get; init; }
            }

            sealed class Inner
            {
                public string Name { get; } = "ok";
            }

            class Sample
            {
                void Execute(Outer nonNull, Outer? maybeNull)
                {
                    {|CB1006:ThrowIfNull(nonNull)|};
                    _ = maybeNull?.Optional?.Name;
                    _ = maybeNull?{|CB1006:.Required?.Name|};
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsConstrainedNonNullableTypeParametersOnlyAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest("""
            #nullable enable
            class BaseItem { }
            interface IMarker { }

            class Sample
            {
                void CheckNotNull<T>(T value) where T : notnull => _ = {|CB1006:value is null|};
                void CheckStruct<T>(T value) where T : struct => _ = {|CB1006:object.Equals(value, null)|};
                void CheckClass<T>(T value) where T : class => {|CB1006:System.ArgumentNullException.ThrowIfNull(value)|};
                void CheckBase<T>(T value) where T : BaseItem => _ = {|CB1006:value ?? value|};
                void CheckInterface<T>(T value) where T : IMarker => _ = {|CB1006:value is null|};

                void AllowUnconstrained<T>(T value) => _ = value is null;
                void AllowNullableClass<T>(T value) where T : class? => _ = value is null;
                void AllowNullableBase<T>(T value) where T : BaseItem? => _ = value is null;
                void AllowNullableInterface<T>(T value) where T : IMarker? => _ = value is null;
                void AllowAnnotatedTypeParam<T>(T? value) where T : class => _ = value is null;
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresNullableTypesStringContentHelpersAndValidPatternsAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest("""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.Linq;

            class BaseType { }
            class DerivedType : BaseType { }

            class Sample
            {
                void Execute(
                    string nonNull,
                    string? maybeNull,
                    string?[] nullableArray,
                    int? maybeInt,
                    BaseType baseValue,
                    Action callback)
                {
                    ArgumentException.ThrowIfNullOrEmpty(nonNull);
                    ArgumentException.ThrowIfNullOrWhiteSpace(nonNull);
                    _ = string.IsNullOrEmpty(nonNull);
                    _ = string.IsNullOrWhiteSpace(nonNull);
                    _ = nullableArray[0] is null;

                    ArgumentNullException.ThrowIfNull(maybeNull);
                    _ = maybeNull == null;
                    _ = maybeNull != null;
                    _ = maybeNull is null;
                    _ = maybeNull is not null;
                    _ = maybeNull is { };
                    _ = maybeNull ?? "fallback";
                    maybeNull ??= "fallback";
                    _ = maybeNull?.Length;
                    _ = maybeNull?.Trim() ?? "fallback";
                    _ = maybeNull?.Equals(null);

                    _ = maybeInt is null;
                    _ = maybeInt ?? 0;
                    _ = maybeInt?.ToString();

                    _ = nonNull is "constant";
                    _ = nonNull is { Length: > 0 };
                    _ = nonNull is string copy ? copy : "";
                    _ = (1, 2) is (1, 2);
                    _ = baseValue is DerivedType { };
                    _ = ReferenceEquals(nonNull, baseValue);
                    _ = ReferenceEquals(null, null);
                    _ = nonNull == "other";
                    _ = null == (string?)null;
                    callback();

                    if (!TryParse(maybeNull, out string? parsed) || parsed is null)
                    {
                        return;
                    }

                    IEnumerable<string> sequence = [nonNull];
                    _ = sequence.FirstOrDefault() ?? "fallback";
                    _ = FindDefault<string>() is null;
                    Dictionary<string, string> map = new();
                    if (!map.TryGetValue(nonNull, out var mapped) || mapped is null)
                    {
                        return;
                    }
                }

                [return: MaybeNull]
                static T FindDefault<T>() => default;

                static bool TryParse(string? input, [NotNullWhen(true)] out string? result)
                {
                    result = input;
                    return input is not null;
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresUserDefinedEqualityOperatorsAgainstNullAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest("""
            #nullable enable
            #pragma warning disable CS0660, CS0661
            sealed class SentinelRef
            {
                public bool IsEmpty { get; init; }
                public static bool operator ==(SentinelRef left, SentinelRef? right) => right is null && left.IsEmpty;
                public static bool operator !=(SentinelRef left, SentinelRef? right) => !(left == right);
            }

            readonly struct SentinelValue
            {
                public bool IsEmpty { get; init; }
                public static bool operator ==(SentinelValue left, object? right) => right is null && left.IsEmpty;
                public static bool operator !=(SentinelValue left, object? right) => !(left == right);
            }

            class Sample
            {
                void Execute(SentinelRef refValue, SentinelValue structValue)
                {
                    _ = refValue == null;
                    _ = refValue != null;
                    _ = structValue == null;
                    _ = structValue != null;
                }
            }
            """);

        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresObliviousDynamicPointersUnresolvedAndGeneratedCodeAsync()
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = CreateTest(
            """
            #nullable disable
            class ObliviousSample
            {
                public static string Current => null;
                public static string GetItem() => null;

                void Run(string value)
                {
                    System.ArgumentNullException.ThrowIfNull(value);
                    _ = value == null;
                }
            }

            #nullable enable
            class UnresolvedAndSpecialTypes
            {
                static void ThrowIfNull(string custom) => _ = custom.Length;

                unsafe void Run(dynamic dyn, int* ptr, string text)
                {
                    ThrowIfNull(text);
                    UnknownHelper.ThrowIfNull(text);
                    System.ArgumentNullException.ThrowIfNull(unknownParam: text);
                    _ = ObliviousSample.Current?.Length;
                    _ = ObliviousSample.GetItem() == null;
                    _ = dyn == null;
                    _ = ptr == null;
                }
            }
            """,
            CompilerDiagnostics.None);

        test.TestState.Sources.Add((
            "GeneratedSample.g.cs",
            """
            #nullable enable
            class GeneratedSample
            {
                void Run(string value)
                {
                    System.ArgumentNullException.ThrowIfNull(value);
                    _ = value == null;
                }
            }
            """));

        await test.RunAsync();
    }

    private static CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> CreateTest(
        string source,
        CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors)
    {
        CSharpAnalyzerTest<NoNonNullableNullCheckAnalyzer, DefaultVerifier> test = new()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            CompilerDiagnostics = compilerDiagnostics,
            TestCode = source,
        };
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            Project project = solution.GetProject(projectId)!;
            CSharpCompilationOptions options = ((CSharpCompilationOptions)project.CompilationOptions!)
                .WithAllowUnsafe(true);
            return project.WithCompilationOptions(options).Solution;
        });

        return test;
    }
}
