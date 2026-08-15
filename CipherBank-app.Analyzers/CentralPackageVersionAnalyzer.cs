// <copyright file="CentralPackageVersionAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags PackageReference Version attributes outside Directory.Packages.props.
/// Use: High (every compilation). Scope: MSBuild additional files.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CentralPackageVersionAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex PackageVersionAttribute = new(
        @"<PackageReference[^>]*\sVersion=[^>]*>",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.CentralPackageVersion);

    /// <summary>
    /// Registers a compilation action that scans additional MSBuild files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Roslyn analysis context for this compilation.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    /// <summary>
    /// Walks additional files and reports Version attributes on PackageReference.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (AdditionalText file in context.Options.AdditionalFiles)
        {
            ReportPackageVersions(context, file);
        }
    }

    /// <summary>
    /// Reports each PackageReference Version= match in one additional file.
    /// Use: High (per additional file). Scope: this analyzer.
    /// </summary>
    private static void ReportPackageVersions(CompilationAnalysisContext context, AdditionalText file)
    {
        if (!SourcePath.IsMsBuildProjectFile(file.Path) || SourcePath.IsCentralPackageFile(file.Path))
        {
            return;
        }

        SourceText? text = file.GetText(context.CancellationToken);
        if (text is null)
        {
            return;
        }

        string content = text.ToString();
        Match match = PackageVersionAttribute.Match(content);
        while (match.Success)
        {
            context.ReportDiagnostic(CreateDiagnostic(file.Path, text, match.Index, match.Length));
            match = match.NextMatch();
        }
    }

    /// <summary>
    /// Builds CB1001 at the match offset.
    /// Use: Medium (each offender). Scope: this analyzer.
    /// </summary>
    private static Diagnostic CreateDiagnostic(string path, SourceText text, int offset, int length)
    {
        TextSpan matchSpan = new(offset, length);
        Location location = Location.Create(path, matchSpan, text.Lines.GetLinePositionSpan(matchSpan));
        string displayPath = SourcePath.Normalize(path);
        int slash = displayPath.LastIndexOf('/');
        string fileName = slash >= 0 ? displayPath.Substring(slash + 1) : displayPath;
        return Diagnostic.Create(CipherBankDiagnostics.CentralPackageVersion, location, fileName);
    }
}
