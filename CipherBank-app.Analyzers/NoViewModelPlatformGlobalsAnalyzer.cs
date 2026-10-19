// <copyright file="NoViewModelPlatformGlobalsAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Rejects direct MAUI and task-dispatch globals from ViewModels. Detection covers
/// invocations, property reads, and assignments by flagging the root global reference
/// (for example <c>Application.Current</c>). Compiled trees use the semantic model so
/// only the platform type matches, including aliases and partial names. Additional files
/// are still scanned when the MAUI head is not compiled.
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

    /// <summary>
    /// Leftmost identifiers of a namespace qualification. Used only when a receiver
    /// does not bind, so <c>Microsoft.Maui.Controls.Application</c> still matches and
    /// <c>_account.Application</c> does not.
    /// </summary>
    private static readonly HashSet<string> _namespaceRoots = new(StringComparer.Ordinal)
    {
        "ApplicationModel",
        "Controls",
        "DataTransfer",
        "Maui",
        "Microsoft",
        "Storage",
        "System",
        "Tasks",
        "Threading",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.ViewModelPlatformGlobal);

    /// <summary>
    /// Registers semantic-model analysis for compilation trees and a compilation action
    /// for additional files that are not part of the compilation.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Analyzer initialization context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction(AnalyzeSemanticModel);
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    /// <summary>
    /// Reports prohibited globals in one ViewModel compilation tree using its semantic model.
    /// Use: High (every compilation tree). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        SyntaxTree tree = context.SemanticModel.SyntaxTree;
        if (!IsViewModelPath(tree.FilePath))
        {
            return;
        }

        foreach ((SyntaxNode access, string root) in ProhibitedAccesses(
            tree,
            context.SemanticModel,
            context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CipherBankDiagnostics.ViewModelPlatformGlobal,
                access.GetLocation(),
                root));
        }
    }

    /// <summary>
    /// Reports prohibited globals in additional ViewModel C# files that are outside the compilation.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
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

        foreach ((SyntaxNode access, string root) in ProhibitedAccesses(tree, model: null, context.CancellationToken))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CipherBankDiagnostics.ViewModelPlatformGlobal,
                AdditionalSource.CreateLocation(file.Path, text, access.Span),
                root));
        }
    }

    /// <summary>
    /// Yields each member access whose receiver is a prohibited platform global,
    /// reporting once per chain at the root reference. <c>Task</c> only bans <c>Run</c>
    /// so cancellable helpers such as <c>Task.Delay</c> remain available.
    /// Use: High (every ViewModel tree). Scope: this analyzer.
    /// </summary>
    private static IEnumerable<(SyntaxNode Access, string Root)> ProhibitedAccesses(
        SyntaxTree tree,
        SemanticModel? model,
        CancellationToken cancellationToken)
    {
        foreach (SyntaxNode node in tree.GetRoot(cancellationToken).DescendantNodes())
        {
            string? root = ProhibitedRoot(node, model, cancellationToken);
            if (root is null)
            {
                continue;
            }

            yield return (node, root);
        }
    }

    /// <summary>
    /// Returns the prohibited global for one member or conditional access, or null.
    /// A bound receiver must be the platform type (bare, partial, <c>global::</c>, or
    /// aliased). Unbound receivers still match a bare name or a namespace qualification.
    /// <c>Task</c> bans <c>Run</c> and <c>Factory.StartNew</c>.
    /// Use: High (every syntax node in a ViewModel tree). Scope: this analyzer.
    /// </summary>
    /// <param name="node">Candidate member or conditional access.</param>
    /// <param name="model">Semantic model for a compilation tree, or null for an additional file.</param>
    /// <param name="cancellationToken">Token that cancels symbol lookup.</param>
    /// <returns>The prohibited global name, or null when the node is allowed.</returns>
    private static string? ProhibitedRoot(
        SyntaxNode node,
        SemanticModel? model,
        CancellationToken cancellationToken)
    {
        if (!TryReadDirectAccess(node, out ExpressionSyntax? receiver, out string member)
            || receiver is null)
        {
            return null;
        }

        if (string.Equals(member, "StartNew", StringComparison.Ordinal)
            && IsTaskFactory(receiver, model, cancellationToken))
        {
            return "Task";
        }

        string? root = ReceiverGlobalName(receiver, model, cancellationToken);
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
    /// Returns the first member bound by <c>?.</c>, walking the leftmost operand of the
    /// calls, member accesses, element accesses, null-forgiving <c>!</c>, and nested <c>?.</c> chained after it,
    /// as in <c>Task.Factory?.StartNew(...).ContinueWith(...)</c>.
    /// Use: High (each conditional access). Scope: this analyzer.
    /// </summary>
    /// <param name="whenNotNull">The <c>WhenNotNull</c> operand of a conditional access.</param>
    /// <returns>The bound member name, or null when the chain does not start with a member binding.</returns>
    private static string? DirectConditionalMember(ExpressionSyntax whenNotNull)
    {
        ExpressionSyntax? current = whenNotNull;
        while (current is not null and not MemberBindingExpressionSyntax)
        {
            current = current switch
            {
                InvocationExpressionSyntax invocation => invocation.Expression,
                MemberAccessExpressionSyntax access => access.Expression,
                ElementAccessExpressionSyntax element => element.Expression,
                ConditionalAccessExpressionSyntax conditional => conditional.Expression,
                PostfixUnaryExpressionSyntax suppressed
                    when suppressed.IsKind(SyntaxKind.SuppressNullableWarningExpression) => suppressed.Operand,
                _ => null,
            };
        }

        return (current as MemberBindingExpressionSyntax)?.Name.Identifier.ValueText;
    }

    /// <summary>
    /// True when <paramref name="expression"/> is <c>Task.Factory</c>, including a qualified or
    /// <c>global::</c> <c>Task</c>.
    /// Use: High (each <c>StartNew</c> candidate). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver of a potential <c>StartNew</c> access.</param>
    /// <param name="model">Semantic model for a compilation tree, or null for an additional file.</param>
    /// <param name="cancellationToken">Token that cancels symbol lookup.</param>
    /// <returns>True when the receiver is the task factory.</returns>
    private static bool IsTaskFactory(
        ExpressionSyntax expression,
        SemanticModel? model,
        CancellationToken cancellationToken)
    {
        if (expression is not MemberAccessExpressionSyntax factory
            || !string.Equals(factory.Name.Identifier.ValueText, "Factory", StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(
            ReceiverGlobalName(factory.Expression, model, cancellationToken),
            "Task",
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Names the platform global a receiver refers to.
    /// Bound symbols match only a prohibited platform type (the MAUI type or
    /// <c>System.Threading.Tasks.Task</c>), so an instance member, field, local, or
    /// parameter with the same name is ignored. Unbound receivers fall back to a bare
    /// identifier, <c>global::</c> name, or namespace qualification.
    /// Use: High (each member receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver of a member or conditional access.</param>
    /// <param name="model">Semantic model for a compilation tree, or null for an additional file.</param>
    /// <param name="cancellationToken">Token that cancels symbol lookup.</param>
    /// <returns>The prohibited global name, or null when the receiver is not one.</returns>
    private static string? ReceiverGlobalName(
        ExpressionSyntax expression,
        SemanticModel? model,
        CancellationToken cancellationToken)
    {
        if (model is not null
            && TryResolveBoundRoot(model, expression, cancellationToken, out string? boundRoot))
        {
            return boundRoot;
        }

        return SyntacticQualificationName(expression);
    }

    /// <summary>
    /// Resolves a receiver that binds to a real symbol.
    /// Use: High (each member receiver in a compilation tree). Scope: this analyzer.
    /// </summary>
    /// <param name="model">Semantic model for the receiver.</param>
    /// <param name="expression">Receiver of a member or conditional access.</param>
    /// <param name="cancellationToken">Token that cancels symbol lookup.</param>
    /// <param name="root">
    /// Platform type name when the symbol is a prohibited type; null when the symbol
    /// is some other real symbol.
    /// </param>
    /// <returns>False when the receiver does not bind and syntax should be used instead.</returns>
    private static bool TryResolveBoundRoot(
        SemanticModel model,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out string? root)
    {
        root = null;
        SymbolInfo info = model.GetSymbolInfo(expression, cancellationToken);
        if (info.Symbol is ISymbol symbol)
        {
            if (!IsResolvable(symbol))
            {
                return false;
            }

            root = PlatformRoot(symbol);
            return true;
        }

        bool anyResolvable = false;
        foreach (ISymbol candidate in info.CandidateSymbols)
        {
            if (!IsResolvable(candidate))
            {
                continue;
            }

            anyResolvable = true;
            string? match = PlatformRoot(candidate);
            if (match is not null)
            {
                root = match;
                return true;
            }
        }

        return anyResolvable;
    }

    /// <summary>
    /// True when <paramref name="symbol"/> is a real binding rather than an error type.
    /// Use: High (each symbol lookup). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Symbol returned for a receiver.</param>
    /// <returns>False for error types, which fall back to syntax.</returns>
    private static bool IsResolvable(ISymbol symbol)
        => symbol is not IErrorTypeSymbol;

    /// <summary>
    /// Returns the prohibited root when <paramref name="symbol"/> is a platform type
    /// such as <c>Microsoft.Maui.Controls.Application</c> or <c>Microsoft.Maui.Storage.Preferences</c>.
    /// Aliases resolve to their target. Static members are reported through the type of
    /// their receiver, not as a separate root.
    /// Use: High (each bound receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Bound receiver symbol.</param>
    /// <returns>The prohibited global name, or null when the symbol is not a platform type.</returns>
    private static string? PlatformRoot(ISymbol symbol)
    {
        if (symbol is IAliasSymbol alias)
        {
            symbol = alias.Target;
        }

        if (symbol is not INamedTypeSymbol type || type.ContainingType is not null)
        {
            return null;
        }

        string name = type.Name;
        bool match = name switch
        {
            "Application" or "Shell" => HasExactNamespace(type, "Microsoft.Maui.Controls"),
            "Clipboard" => HasExactNamespace(type, "Microsoft.Maui.ApplicationModel.DataTransfer"),
            "MainThread" => HasExactNamespace(type, "Microsoft.Maui.ApplicationModel"),
            "Preferences" or "SecureStorage" => HasExactNamespace(type, "Microsoft.Maui.Storage"),
            "Task" => HasExactNamespace(type, "System.Threading.Tasks"),
            _ => false,
        };

        return match ? name : null;
    }

    /// <summary>
    /// True when <paramref name="type"/> sits directly in <paramref name="namespaceName"/>.
    /// Use: High (each bound platform-type check). Scope: this analyzer.
    /// </summary>
    /// <param name="type">Candidate platform type.</param>
    /// <param name="namespaceName">Expected dotted namespace.</param>
    /// <returns>True when the containing namespace matches exactly.</returns>
    private static bool HasExactNamespace(INamedTypeSymbol type, string namespaceName)
    {
        int end = namespaceName.Length;
        INamespaceSymbol? ns = type.ContainingNamespace;
        while (ns is not null && !ns.IsGlobalNamespace)
        {
            string name = ns.Name;
            if (end < name.Length
                || !string.Equals(
                    namespaceName.Substring(end - name.Length, name.Length),
                    name,
                    StringComparison.Ordinal))
            {
                return false;
            }

            end -= name.Length;
            if (end == 0)
            {
                return ns.ContainingNamespace is null || ns.ContainingNamespace.IsGlobalNamespace;
            }

            if (namespaceName[end - 1] != '.')
            {
                return false;
            }

            end--;
            ns = ns.ContainingNamespace;
        }

        return false;
    }

    /// <summary>
    /// Reads a receiver that is syntactically a namespace or type name.
    /// Use: High (each unbound member receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver of a member or conditional access.</param>
    /// <returns>
    /// Bare identifier text, the name after <c>::</c>, or the rightmost identifier of a
    /// namespace qualification such as <c>Microsoft.Maui.Controls.Application</c>.
    /// Instance member chains return null.
    /// </returns>
    private static string? SyntacticQualificationName(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        if (expression is AliasQualifiedNameSyntax alias)
        {
            return alias.Name.Identifier.ValueText;
        }

        if (expression is MemberAccessExpressionSyntax member && IsNamespaceOrTypeQualification(member))
        {
            return member.Name.Identifier.ValueText;
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="qualification"/> is a namespace or type name, not an instance chain.
    /// Use: High (each unbound member receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="qualification">Member-access receiver such as <c>Microsoft.Maui.Controls.Application</c>.</param>
    /// <returns>True for <c>global::</c> and known namespace roots; false for <c>_account.Application</c>.</returns>
    private static bool IsNamespaceOrTypeQualification(MemberAccessExpressionSyntax qualification)
    {
        ExpressionSyntax current = qualification;
        while (current is MemberAccessExpressionSyntax access)
        {
            if (!access.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            current = access.Expression;
        }

        return current switch
        {
            AliasQualifiedNameSyntax => true,
            IdentifierNameSyntax identifier => _namespaceRoots.Contains(identifier.Identifier.ValueText),
            _ => false,
        };
    }

    /// <summary>
    /// True when the path sits below a ViewModels directory.
    /// Use: High (every tree and additional file). Scope: this analyzer.
    /// </summary>
    private static bool IsViewModelPath(string path)
        => path.Replace('\\', '/').Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase);
}
