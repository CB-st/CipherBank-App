// <copyright file="NoRetiredApiNamesAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags retired API identifiers IProductApi, MockProductApi, and AppSessionDeps.
/// Use: High (every compilation). Scope: C# identifier tokens.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoRetiredApiNamesAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> RetiredNames = new(StringComparer.Ordinal)
    {
        // TODO: If this list gets updated - change it to an ingestible config file.
        "IProductApi",
        "MockProductApi",
        "AppSessionDeps",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.RetiredApiName);

    /// <summary>
    /// Registers a compilation action over compilation trees and additional files.
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
    /// Reports retired identifiers in compilation trees and additional C# files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            ReportTree(context.ReportDiagnostic, tree, context.CancellationToken);
        }

        foreach (AdditionalText file in context.Options.AdditionalFiles)
        {
            ReportAdditionalFile(context, file);
        }
    }

    /// <summary>
    /// Reports each retired identifier token in one syntax tree.
    /// Use: High (every C# tree). Scope: this analyzer.
    /// </summary>
    private static void ReportTree(Action<Diagnostic> report, SyntaxTree tree, CancellationToken cancellationToken)
    {
        foreach (SyntaxToken token in tree.GetRoot(cancellationToken).DescendantTokens())
        {
            ReportIfRetired(report, token);
        }
    }

    /// <summary>
    /// Reports retired identifiers in one additional C# file.
    /// Use: High (every additional file). Scope: unbuilt sibling projects.
    /// </summary>
    private static void ReportAdditionalFile(CompilationAnalysisContext context, AdditionalText file)
    {
        if (!AdditionalSource.IsOutsideCompilation(context.Compilation, file.Path))
        {
            return;
        }

        SyntaxTree tree;
        SourceText text;
        if (!AdditionalSource.TryParseCSharp(file, context.CancellationToken, out tree, out text))
        {
            return;
        }

        foreach (SyntaxToken token in tree.GetRoot(context.CancellationToken).DescendantTokens())
        {
            ReportAdditionalIfRetired(context, file.Path, text, token);
        }
    }

    /// <summary>
    /// Reports CB1004 for a retired identifier in an additional C# file.
    /// Use: High (every identifier token). Scope: unbuilt sibling projects.
    /// </summary>
    private static void ReportAdditionalIfRetired(
        CompilationAnalysisContext context,
        string path,
        SourceText text,
        SyntaxToken token)
    {
        if (!token.IsKind(SyntaxKind.IdentifierToken) || !RetiredNames.Contains(token.ValueText))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.RetiredApiName,
            AdditionalSource.CreateLocation(path, text, token.Span),
            token.ValueText));
    }

    /// <summary>
    /// Reports CB1004 when the token is a retired identifier.
    /// Use: High (every identifier token). Scope: this analyzer.
    /// </summary>
    private static void ReportIfRetired(Action<Diagnostic> report, SyntaxToken token)
    {
        if (!token.IsKind(SyntaxKind.IdentifierToken) || !RetiredNames.Contains(token.ValueText))
        {
            return;
        }

        report(Diagnostic.Create(
            CipherBankDiagnostics.RetiredApiName,
            token.GetLocation(),
            token.ValueText));
    }
}
