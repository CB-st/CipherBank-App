// <copyright file="CentralPackageVersionAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
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
    private const string PackageReferenceTag = "<PackageReference";
    private const string VersionAttribute = " Version=";
    private const string TabVersionAttribute = "\tVersion=";

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
        int search = 0;
        while (search < content.Length)
        {
            int tagStart = content.IndexOf(PackageReferenceTag, search, StringComparison.Ordinal);
            if (tagStart < 0)
            {
                return;
            }

            int tagEnd = content.IndexOf('>', tagStart);
            if (tagEnd < 0)
            {
                return;
            }

            int tagLength = tagEnd - tagStart + 1;
            string tag = content.Substring(tagStart, tagLength);
            if (tag.IndexOf(VersionAttribute, StringComparison.Ordinal) >= 0
                || tag.IndexOf(TabVersionAttribute, StringComparison.Ordinal) >= 0)
            {
                context.ReportDiagnostic(CreateDiagnostic(file.Path, text, tagStart, tagLength));
            }

            search = tagEnd + 1;
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
