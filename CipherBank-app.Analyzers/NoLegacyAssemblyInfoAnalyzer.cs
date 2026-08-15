// <copyright file="NoLegacyAssemblyInfoAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
    /// Registers a compilation action that inspects trees and additional files.
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
    /// Reports CB1002 for compilation trees and additional AssemblyInfo files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            ReportCompilationTree(context, tree);
        }

        foreach (AdditionalText file in context.Options.AdditionalFiles)
        {
            ReportAdditionalAssemblyInfo(context, file);
        }
    }

    /// <summary>
    /// Reports CB1002 when the tree is Properties/AssemblyInfo.cs.
    /// Use: High (every C# tree). Scope: this analyzer.
    /// </summary>
    private static void ReportCompilationTree(CompilationAnalysisContext context, SyntaxTree tree)
    {
        if (!SourcePath.IsLegacyAssemblyInfo(tree.FilePath))
        {
            return;
        }

        Location location = tree.GetRoot(context.CancellationToken).GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(CipherBankDiagnostics.LegacyAssemblyInfo, location));
    }

    /// <summary>
    /// Reports one additional AssemblyInfo.cs that is not already a syntax tree.
    /// Use: High (every additional file). Scope: unbuilt sibling projects.
    /// </summary>
    private static void ReportAdditionalAssemblyInfo(CompilationAnalysisContext context, AdditionalText file)
    {
        if (!SourcePath.IsLegacyAssemblyInfo(file.Path)
            || !AdditionalSource.IsOutsideCompilation(context.Compilation, file.Path))
        {
            return;
        }

        SourceText? text = file.GetText(context.CancellationToken);
        if (text is null)
        {
            return;
        }

        TextSpan span = new(0, text.Length);
        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.LegacyAssemblyInfo,
            AdditionalSource.CreateLocation(file.Path, text, span)));
    }
}
