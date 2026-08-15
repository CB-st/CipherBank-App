// <copyright file="NoRetiredApiNamesAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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
        "IProductApi",
        "MockProductApi",
        "AppSessionDeps",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.RetiredApiName);

    /// <summary>
    /// Registers a syntax-tree walk over identifier tokens.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Roslyn analysis context for this compilation.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    /// <summary>
    /// Reports each retired identifier token in the tree.
    /// Use: High (every C# tree). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        foreach (SyntaxToken token in context.Tree.GetRoot(context.CancellationToken).DescendantTokens())
        {
            ReportIfRetired(context, token);
        }
    }

    /// <summary>
    /// Reports CB1004 when the token is a retired identifier.
    /// Use: High (every identifier token). Scope: this analyzer.
    /// </summary>
    private static void ReportIfRetired(SyntaxTreeAnalysisContext context, SyntaxToken token)
    {
        if (!token.IsKind(SyntaxKind.IdentifierToken) || !RetiredNames.Contains(token.ValueText))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.RetiredApiName,
            token.GetLocation(),
            token.ValueText));
    }
}
