// <copyright file="NoLegacyAssemblyInfoAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags Properties/AssemblyInfo.cs trees that SDK-style projects must not compile.
/// Use: High (every compilation). Scope: C# syntax trees.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoLegacyAssemblyInfoAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.LegacyAssemblyInfo);

    /// <summary>
    /// Registers a compilation-start action that inspects each syntax tree path.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Roslyn analysis context for this compilation.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            start.RegisterSyntaxTreeAction(AnalyzeTree);
        });
    }

    /// <summary>
    /// Reports CB1002 when the tree is Properties/AssemblyInfo.cs.
    /// Use: High (every C# tree). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        if (!SourcePath.IsLegacyAssemblyInfo(context.Tree.FilePath))
        {
            return;
        }

        Location location = context.Tree.GetRoot(context.CancellationToken).GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(CipherBankDiagnostics.LegacyAssemblyInfo, location));
    }
}
