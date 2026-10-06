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
    private static readonly HashSet<string> _prohibitedRoots = new(StringComparer.Ordinal)
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

            foreach ((SyntaxNode access, string root) in ProhibitedAccesses(tree, context.CancellationToken))
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

        foreach ((SyntaxNode access, string root) in ProhibitedAccesses(tree, context.CancellationToken))
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
    private static IEnumerable<(SyntaxNode Access, string Root)> ProhibitedAccesses(
        SyntaxTree tree,
        CancellationToken cancellationToken)
    {
        foreach (SyntaxNode node in tree.GetRoot(cancellationToken).DescendantNodes())
        {
            string? root = ProhibitedRoot(node);
            if (root is null)
            {
                continue;
            }

            yield return (node, root);
        }
    }

    /// <summary>
    /// Returns the prohibited global for one member or conditional access, or null.
    /// Qualified and <c>global::</c> receivers contribute the identifier that names the
    /// global. <c>Task</c> bans <c>Run</c> and <c>Factory.StartNew</c>.
    /// Use: High (every syntax node in a ViewModel tree). Scope: this analyzer.
    /// </summary>
    /// <param name="node">Candidate member or conditional access.</param>
    /// <returns>The prohibited global name, or null when the node is allowed.</returns>
    private static string? ProhibitedRoot(SyntaxNode node)
    {
        if (!TryReadDirectAccess(node, out ExpressionSyntax? receiver, out string member)
            || receiver is null)
        {
            return null;
        }

        if (string.Equals(member, "StartNew", StringComparison.Ordinal) && IsTaskFactory(receiver))
        {
            return "Task";
        }

        string? root = ReceiverGlobalName(receiver);
        if (root is null || !_prohibitedRoots.Contains(root))
        {
            return null;
        }

        if (string.Equals(root, "Task", StringComparison.Ordinal)
            && !string.Equals(member, "Run", StringComparison.Ordinal))
        {
            return null;
        }

        return root;
    }

    /// <summary>
    /// Reads the receiver and the member name directly accessed on it.
    /// Use: High (every syntax node in a ViewModel tree). Scope: this analyzer.
    /// </summary>
    /// <param name="node">Candidate member or conditional access.</param>
    /// <param name="receiver">Expression that names the global, when the read succeeds.</param>
    /// <param name="member">Member name taken from that receiver.</param>
    /// <returns>True when <paramref name="node"/> is a member or conditional access.</returns>
    private static bool TryReadDirectAccess(SyntaxNode node, out ExpressionSyntax? receiver, out string member)
    {
        if (node is MemberAccessExpressionSyntax access)
        {
            receiver = access.Expression;
            member = access.Name.Identifier.ValueText;
            return true;
        }

        if (node is ConditionalAccessExpressionSyntax conditional
            && DirectConditionalMember(conditional.WhenNotNull) is string conditionalMember)
        {
            receiver = conditional.Expression;
            member = conditionalMember;
            return true;
        }

        receiver = null;
        member = string.Empty;
        return false;
    }

    /// <summary>
    /// Returns the first member bound by <c>?.</c>, skipping a call around that member.
    /// Use: High (each conditional access). Scope: this analyzer.
    /// </summary>
    /// <param name="whenNotNull">The <c>WhenNotNull</c> operand of a conditional access.</param>
    /// <returns>The bound member name, or null when the shape is not a member access.</returns>
    private static string? DirectConditionalMember(ExpressionSyntax whenNotNull)
    {
        ExpressionSyntax current = whenNotNull;
        while (current is InvocationExpressionSyntax invocation)
        {
            current = invocation.Expression;
        }

        if (current is MemberBindingExpressionSyntax binding)
        {
            return binding.Name.Identifier.ValueText;
        }

        if (current is MemberAccessExpressionSyntax access
            && access.Expression is MemberBindingExpressionSyntax nested)
        {
            return nested.Name.Identifier.ValueText;
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="expression"/> is <c>Task.Factory</c>, including a qualified or
    /// <c>global::</c> <c>Task</c>.
    /// Use: High (each <c>StartNew</c> candidate). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver of a potential <c>StartNew</c> access.</param>
    /// <returns>True when the receiver is the task factory.</returns>
    private static bool IsTaskFactory(ExpressionSyntax expression)
    {
        if (expression is not MemberAccessExpressionSyntax factory
            || !string.Equals(factory.Name.Identifier.ValueText, "Factory", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(ReceiverGlobalName(factory.Expression), "Task", StringComparison.Ordinal);
    }

    /// <summary>
    /// Walks a qualified or alias-qualified receiver to the identifier that names the global.
    /// Use: High (each member receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver of a member or conditional access.</param>
    /// <returns>
    /// Bare identifier text, the name after <c>::</c>, or the rightmost identifier of a
    /// qualification such as <c>Microsoft.Maui.Controls.Application</c>.
    /// </returns>
    private static string? ReceiverGlobalName(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        if (expression is AliasQualifiedNameSyntax alias)
        {
            return alias.Name.Identifier.ValueText;
        }

        if (expression is MemberAccessExpressionSyntax member)
        {
            return member.Name.Identifier.ValueText;
        }

        return null;
    }

    /// <summary>
    /// True when the path sits below a ViewModels directory.
    /// Use: High (every tree and additional file). Scope: this analyzer.
    /// </summary>
    private static bool IsViewModelPath(string path)
        => path.Replace('\\', '/').Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase);
}
