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
    private const string VersionName = "Version";

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
        if (!file.Path.IsMsBuildProjectFile() || file.Path.IsCentralPackageFile())
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
            if (HasVersionAttribute(tag))
            {
                context.ReportDiagnostic(CreateDiagnostic(file.Path, text, tagStart, tagLength));
            }

            search = tagEnd + 1;
        }
    }

    /// <summary>
    /// True when a PackageReference tag has a Version attribute after XML whitespace.
    /// Use: High (every PackageReference tag). Scope: this analyzer.
    /// </summary>
    private static bool HasVersionAttribute(string tag)
    {
        int search = 0;
        while (search < tag.Length)
        {
            int nameAt = tag.IndexOf(VersionName, search, StringComparison.Ordinal);
            if (nameAt < 0)
            {
                return false;
            }

            if (IsVersionAttributeAt(tag, nameAt))
            {
                return true;
            }

            search = nameAt + 1;
        }

        return false;
    }

    /// <summary>
    /// True when <paramref name="nameAt"/> is a Version attribute, not a name substring.
    /// Use: High (each Version match). Scope: this analyzer.
    /// </summary>
    private static bool IsVersionAttributeAt(string tag, int nameAt)
    {
        bool precededByWhitespace = nameAt > 0 && char.IsWhiteSpace(tag[nameAt - 1]);
        int equalsAt = SkipXmlWhitespace(tag, nameAt + VersionName.Length);
        return precededByWhitespace && equalsAt < tag.Length && tag[equalsAt] == '=';
    }

    /// <summary>
    /// Advances past XML whitespace (space, tab, CR, LF, and other IsWhiteSpace).
    /// Use: High (each Version candidate). Scope: this analyzer.
    /// </summary>
    private static int SkipXmlWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Builds CB1001 at the match offset.
    /// Use: Medium (each offender). Scope: this analyzer.
    /// </summary>
    private static Diagnostic CreateDiagnostic(
        string path,
        SourceText text,
        int offset,
        int length)
    {
        TextSpan matchSpan = new(offset, length);
        Location location = Location.Create(path, matchSpan, text.Lines.GetLinePositionSpan(matchSpan));
        return Diagnostic.Create(
            CipherBankDiagnostics.CentralPackageVersion,
            location,
            Path.GetFileName(path));
    }
}
