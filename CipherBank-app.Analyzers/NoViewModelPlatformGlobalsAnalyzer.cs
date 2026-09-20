// <copyright file="NoViewModelPlatformGlobalsAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Rejects direct MAUI and task-dispatch globals from ViewModels. Detection covers
/// invocations, property reads, and assignments by flagging the root global reference
/// (for example <c>Application.Current</c>). Like the other structure analyzers, it runs
/// as a compilation action over both compiled trees and AdditionalFiles so the CI
/// structure pass sees ViewModels even when the MAUI head is not compiled.
/// Use: High (every compilation). Scope: files below a ViewModels directory.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoViewModelPlatformGlobalsAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> ProhibitedRoots = new(StringComparer.Ordinal)
    {
        "Application",
        "Clipboard",
        "MainThread",
        "Preferences",
        "SecureStorage",
        "Shell",
        "Task",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.ViewModelPlatformGlobal);

    /// <summary>
    /// Registers a compilation action over compilation trees and additional files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Analyzer initialization context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    /// <summary>
    /// Reports prohibited globals in ViewModel compilation trees and additional C# files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
        {
            if (!IsViewModelPath(tree.FilePath))
            {
                continue;
            }

            foreach ((MemberAccessExpressionSyntax access, string root) in ProhibitedAccesses(tree, context.CancellationToken))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CipherBankDiagnostics.ViewModelPlatformGlobal,
                    access.GetLocation(),
                    root));
            }
        }

        foreach (AdditionalText file in context.Options.AdditionalFiles)
        {
            ReportAdditionalFile(context, file);
        }
    }

    /// <summary>
    /// Reports prohibited globals in one additional ViewModel C# file.
    /// Use: High (every additional file). Scope: unbuilt sibling projects.
    /// </summary>
    private static void ReportAdditionalFile(CompilationAnalysisContext context, AdditionalText file)
    {
        if (!IsViewModelPath(file.Path)
            || !AdditionalSource.IsOutsideCompilation(context.Compilation, file.Path))
        {
            return;
        }

        SyntaxTree tree;
        SourceText text;
        if (!AdditionalSource.TryParseCSharp(file, context.CancellationToken, out tree, out text))
        {
            return;
        }

        foreach ((MemberAccessExpressionSyntax access, string root) in ProhibitedAccesses(tree, context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CipherBankDiagnostics.ViewModelPlatformGlobal,
                AdditionalSource.CreateLocation(file.Path, text, access.Span),
                root));
        }
    }

    /// <summary>
    /// Yields each member access whose leftmost identifier is a prohibited global,
    /// reporting once per chain at the root reference. <c>Task</c> only bans <c>Run</c>
    /// so cancellable helpers such as <c>Task.Delay</c> remain available.
    /// Use: High (every ViewModel tree). Scope: this analyzer.
    /// </summary>
    private static IEnumerable<(MemberAccessExpressionSyntax Access, string Root)> ProhibitedAccesses(
        SyntaxTree tree,
        CancellationToken cancellationToken)
    {
        foreach (SyntaxNode node in tree.GetRoot(cancellationToken).DescendantNodes())
        {
            if (node is not MemberAccessExpressionSyntax access
                || access.Expression is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            string root = identifier.Identifier.ValueText;
            if (!ProhibitedRoots.Contains(root))
            {
                continue;
            }

            if (string.Equals(root, "Task", StringComparison.Ordinal)
                && !string.Equals(access.Name.Identifier.ValueText, "Run", StringComparison.Ordinal))
            {
                continue;
            }

            yield return (access, root);
        }
    }

    /// <summary>
    /// True when the path sits below a ViewModels directory.
    /// Use: High (every tree and additional file). Scope: this analyzer.
    /// </summary>
    private static bool IsViewModelPath(string path)
        => path.Replace('\\', '/').Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase);
}
