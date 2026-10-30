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

    private static readonly Dictionary<string, string> _platformNamespaces = new(StringComparer.Ordinal)
    {
        ["Application"] = "Microsoft.Maui.Controls",
        ["Clipboard"] = "Microsoft.Maui.ApplicationModel.DataTransfer",
        ["MainThread"] = "Microsoft.Maui.ApplicationModel",
        ["Preferences"] = "Microsoft.Maui.Storage",
        ["SecureStorage"] = "Microsoft.Maui.Storage",
        ["Shell"] = "Microsoft.Maui.Controls",
        ["Task"] = "System.Threading.Tasks",
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

    /// <summary>
    /// Namespace paths a using directive can open for a partial root in additional files.
    /// </summary>
    private static readonly HashSet<string> _knownPlatformNamespaces = new(StringComparer.Ordinal)
    {
        "Microsoft.Maui",
        "Microsoft.Maui.ApplicationModel",
        "Microsoft.Maui.ApplicationModel.DataTransfer",
        "Microsoft.Maui.Controls",
        "Microsoft.Maui.Storage",
        "System.Threading",
        "System.Threading.Tasks",
    };

    /// <summary>
    /// Roots accepted in additional files, where nothing binds: only fully qualified namespaces.
    /// </summary>
    private static readonly HashSet<string> _fullyQualifiedRoots = new(StringComparer.Ordinal)
    {
        "Microsoft",
        "System",
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
    /// Yields each member or conditional access whose receiver is a prohibited platform global,
    /// reporting once per chain at the root reference. <c>Task.Run</c> and <c>Task.Factory.StartNew</c>
    /// are prohibited; cancellable helpers such as <c>Task.Delay</c> remain allowed.
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
        expression = UnwrapParentheses(expression);
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
        expression = UnwrapParentheses(expression);
        if (model is not null
            && TryResolveBoundRoot(model, expression, cancellationToken, out string? boundRoot))
        {
            return boundRoot;
        }

        return SyntacticQualificationName(expression, fullyQualifiedRootsOnly: model is null);
    }

    /// <summary>
    /// Removes parentheses surrounding a receiver before classifying it.
    /// Use: High (each member receiver). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Receiver expression.</param>
    /// <returns>The innermost expression.</returns>
    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
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
        bool match = _platformNamespaces.TryGetValue(name, out string? namespaceName)
            && HasExactNamespace(type, namespaceName);

        return match ? name : null;
    }

    /// <summary>
    /// True when <paramref name="symbol"/> sits directly in <paramref name="namespaceName"/>.
    /// Use: High (each bound platform-type check). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Candidate platform symbol.</param>
    /// <param name="namespaceName">Expected dotted namespace.</param>
    /// <returns>True when the containing namespace matches exactly.</returns>
    private static bool HasExactNamespace(ISymbol symbol, string namespaceName)
    {
        int end = namespaceName.Length;
        INamespaceSymbol? ns = symbol.ContainingNamespace;
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
    /// Bare identifier text, the name of a generic type such as <c>Task&lt;int&gt;</c>,
    /// the name after <c>::</c>, or the rightmost identifier of a namespace qualification
    /// such as <c>System.Threading.Tasks.Task&lt;int&gt;</c>. Instance member chains return null.
    /// </returns>
    /// <param name="fullyQualifiedRootsOnly">
    /// True for additional files (no semantic model): only <c>global::</c>, <c>Microsoft</c> and
    /// <c>System</c> roots count, because partial roots such as <c>Controls</c> are ambiguous with
    /// instance chains like <c>Controls.Application.Current</c>.
    /// </param>
    private static string? SyntacticQualificationName(ExpressionSyntax expression, bool fullyQualifiedRootsOnly)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        // Additional files have no semantic model. Task<int>.Run is a generic name, not an identifier.
        if (expression is GenericNameSyntax generic)
        {
            return generic.Identifier.ValueText;
        }

        if (expression is AliasQualifiedNameSyntax alias)
        {
            return alias.Name.Identifier.ValueText;
        }

        if (expression is MemberAccessExpressionSyntax member && IsNamespaceOrTypeQualification(member, fullyQualifiedRootsOnly))
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
    /// <param name="fullyQualifiedRootsOnly">When true, accept only <c>global::</c> and fully qualified roots.</param>
    /// <returns>True for <c>global::</c> and known namespace roots; false for <c>_account.Application</c>.</returns>
    private static bool IsNamespaceOrTypeQualification(MemberAccessExpressionSyntax qualification, bool fullyQualifiedRootsOnly)
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
            IdentifierNameSyntax identifier => fullyQualifiedRootsOnly
                ? _fullyQualifiedRoots.Contains(identifier.Identifier.ValueText)
                    || IsImportedNamespaceRoot(identifier)
                : _namespaceRoots.Contains(identifier.Identifier.ValueText),
            _ => false,
        };
    }

    /// <summary>
    /// True when a using directive in scope turns <paramref name="root"/> into a known platform
    /// namespace path, such as <c>Controls</c> under <c>using Microsoft.Maui;</c>.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    /// <param name="root">Leftmost identifier of a member-access receiver.</param>
    /// <returns>False for member-rooted chains such as <c>Controls.Application</c> with no matching import.</returns>
    private static bool IsImportedNamespaceRoot(SimpleNameSyntax root)
    {
        string name = root.Identifier.ValueText;
        if (DeclaresValueNamed(root, name))
        {
            return false;
        }

        return root.Ancestors()
            .SelectMany(ScopeUsings)
            .Any(directive => directive.Alias is null
                && directive.StaticKeyword.IsKind(SyntaxKind.None)
                && directive.Name is not null
                && _knownPlatformNamespaces.Contains(ImportedNamespace(directive.Name) + "." + name));
    }

    /// <summary>
    /// Using directives declared directly by a compilation unit or namespace.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static IEnumerable<UsingDirectiveSyntax> ScopeUsings(SyntaxNode scope)
        => scope switch
        {
            CompilationUnitSyntax unit => unit.Usings,
            BaseNamespaceDeclarationSyntax ns => ns.Usings,
            _ => Enumerable.Empty<UsingDirectiveSyntax>(),
        };

    /// <summary>
    /// True when a value named <paramref name="name"/> is in scope at <paramref name="node"/>:
    /// a member of an enclosing type, a parameter of an enclosing method, local function or lambda,
    /// or a local, pattern or loop variable declared in an enclosing block or statement.
    /// C# binds such a value before an imported namespace. Declarations in other methods or
    /// blocks do not count.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static bool DeclaresValueNamed(SyntaxNode node, string name)
        => node.Ancestors().Any(scope => ScopeDeclaresValue(scope, name));

    /// <summary>
    /// True when <paramref name="scope"/> itself introduces a value named <paramref name="name"/>.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static bool ScopeDeclaresValue(SyntaxNode scope, string name)
        => scope switch
        {
            TypeDeclarationSyntax type => TypeDeclaresValue(type, name),
            BaseMethodDeclarationSyntax method => HasParameter(method.ParameterList, name),
            LocalFunctionStatementSyntax local => HasParameter(local.ParameterList, name),
            ParenthesizedLambdaExpressionSyntax lambda => HasParameter(lambda.ParameterList, name),
            SimpleLambdaExpressionSyntax lambda => lambda.Parameter.Identifier.ValueText == name,
            AnonymousMethodExpressionSyntax anonymous => anonymous.ParameterList is not null
                && HasParameter(anonymous.ParameterList, name),
            ForEachStatementSyntax loop => loop.Identifier.ValueText == name,
            BlockSyntax block => block.Statements.Any(statement => StatementDeclaresValue(statement, name)),
            SwitchSectionSyntax section => section.Statements.Any(statement => StatementDeclaresValue(statement, name)),
            _ => false,
        };

    /// <summary>
    /// True when a type declares a field, property, event or primary-constructor parameter with that name.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static bool TypeDeclaresValue(TypeDeclarationSyntax type, string name)
        => (type.ParameterList is not null && HasParameter(type.ParameterList, name))
            || type.Members.Any(member => member switch
            {
                BaseFieldDeclarationSyntax field => field.Declaration.Variables.Any(variable => variable.Identifier.ValueText == name),
                PropertyDeclarationSyntax property => property.Identifier.ValueText == name,
                EventDeclarationSyntax evt => evt.Identifier.ValueText == name,
                _ => false,
            });

    /// <summary>
    /// True when a statement declares a local or pattern variable with that name, without
    /// looking into nested blocks or lambdas, which have their own scopes.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static bool StatementDeclaresValue(StatementSyntax statement, string name)
        => statement
            .DescendantNodesAndSelf(child => child == statement
                || child is not (BlockSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
            .Any(declaration => declaration switch
            {
                VariableDeclaratorSyntax variable => variable.Identifier.ValueText == name,
                SingleVariableDesignationSyntax designation => designation.Identifier.ValueText == name,
                LocalFunctionStatementSyntax function => function.Identifier.ValueText == name,
                _ => false,
            });

    /// <summary>
    /// True when a parameter list declares a parameter with that name.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static bool HasParameter(BaseParameterListSyntax parameters, string name)
        => parameters.Parameters.Any(parameter => parameter.Identifier.ValueText == name);

    /// <summary>
    /// Text of a using directive name without a leading <c>global::</c> alias.
    /// Use: Medium (additional-file fallback only). Scope: this analyzer.
    /// </summary>
    private static string ImportedNamespace(NameSyntax name)
    {
        const string GlobalPrefix = "global::";
        string text = name.ToString();
        return text.StartsWith(GlobalPrefix, StringComparison.Ordinal) ? text.Substring(GlobalPrefix.Length) : text;
    }

    /// <summary>
    /// True when the path sits below a ViewModels directory.
    /// Use: High (every tree and additional file). Scope: this analyzer.
    /// </summary>
    private static bool IsViewModelPath(string path)
        => path.Replace('\\', '/').Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase);
}
