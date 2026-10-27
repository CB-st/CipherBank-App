// <copyright file="NoScatteredSqlAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags raw SQL in CipherBank-app.Core. Delegate aliases are resolved across every part of a partial class.
/// Use: High (every Core compilation). Scope: Core C# trees.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoScatteredSqlAnalyzer : DiagnosticAnalyzer
{
    private const string CommandTextName = "CommandText";
    private const string RelationalAssemblyName = "Microsoft.EntityFrameworkCore.Relational";
    private const string EntityFrameworkNamespace = "Microsoft.EntityFrameworkCore";
    private const string MigrationsNamespace = "Microsoft.EntityFrameworkCore.Migrations";
    private const int MaxFlowDepth = 24;

    private static readonly HashSet<string> _rawSqlMethods = new(StringComparer.Ordinal)
    {
        "FromSql",
        "FromSqlInterpolated",
        "FromSqlRaw",
        "ExecuteSql",
        "ExecuteSqlAsync",
        "ExecuteSqlInterpolated",
        "ExecuteSqlInterpolatedAsync",
        "ExecuteSqlRaw",
        "ExecuteSqlRawAsync",
        "Sql",
        "SqlQuery",
        "SqlQueryRaw",
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(CipherBankDiagnostics.ScatteredSql);

    /// <summary>
    /// Registers assignment, invocation, and delegate-creation actions for Core trees.
    /// Semantic models for every tree are kept until compilation end so a partial class can be traced across files.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Roslyn analysis context for this compilation.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            ConcurrentDictionary<SyntaxTree, SemanticModel> models = new();
            ConcurrentBag<DeferredInvoke> deferred = new();
            start.RegisterSyntaxNodeAction(
                syntaxContext => Remember(models, syntaxContext.SemanticModel),
                SyntaxKind.CompilationUnit);
            start.RegisterSyntaxNodeAction(
                AnalyzeAssignment,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.UnsignedRightShiftAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression);
            start.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInvocation(syntaxContext, deferred),
                SyntaxKind.InvocationExpression);
            start.RegisterOperationAction(AnalyzeDelegateCreation, OperationKind.DelegateCreation);
            start.RegisterCompilationEndAction(
                endContext => ReportDeferred(endContext, models, deferred));
        });
    }

    /// <summary>
    /// Reports CommandText assignments in Core, including null-conditional targets.
    /// Use: High (every assignment). Scope: Core C# trees.
    /// </summary>
    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        if (!ShouldScan(context.Node.SyntaxTree.FilePath))
        {
            return;
        }

        AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)context.Node;
        SyntaxToken? name = CommandTextToken(assignment.Left);
        if (name is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ScatteredSql,
            name.Value.GetLocation(),
            name.Value.ValueText));
    }

    /// <summary>
    /// Reports a resolved EF Core relational raw-SQL call, including a delegate whose target is one.
    /// A partial-class alias whose write is in another file is retried at compilation end.
    /// Use: High (every invocation). Scope: Core C# trees.
    /// </summary>
    /// <param name="context">Syntax analysis context for one invocation.</param>
    /// <param name="deferred">Invocations whose alias crosses a partial-class part that is not bound yet.</param>
    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ConcurrentBag<DeferredInvoke> deferred)
    {
        if (!ShouldScan(context.Node.SyntaxTree.FilePath)
            || context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IInvocationOperation invocation)
        {
            return;
        }

        if (IsEfRelationalRawSql(invocation.TargetMethod))
        {
            Report(context, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name);
            return;
        }

        if (invocation.TargetMethod.MethodKind != MethodKind.DelegateInvoke)
        {
            return;
        }

        FlowState state = new(context.SemanticModel);
        string? name = RawSqlMethodName(
            invocation.Instance,
            state,
            depth: 0,
            context.CancellationToken);
        if (name is not null)
        {
            Report(context, invocation.Syntax.GetLocation(), name);
            return;
        }

        if (state.Incomplete && invocation.Instance is not null)
        {
            deferred.Add(new DeferredInvoke(invocation.Instance, invocation.Syntax.GetLocation()));
        }
    }

    /// <summary>
    /// Reports a method-group conversion onto an EF Core relational raw-SQL method.
    /// Use: High (every delegate creation). Scope: Core C# trees.
    /// </summary>
    private static void AnalyzeDelegateCreation(OperationAnalysisContext context)
    {
        if (!ShouldScan(context.Operation.Syntax.SyntaxTree.FilePath)
            || context.Operation is not IDelegateCreationOperation creation
            || MethodGroup(creation.Target) is not IMethodReferenceOperation methodReference
            || !IsEfRelationalRawSql(methodReference.Method))
        {
            return;
        }

        Report(context, methodReference.Syntax.GetLocation(), methodReference.Method.Name);
    }

    /// <summary>
    /// True when the tree is Core.
    /// Use: High (every SQL syntax action). Scope: this analyzer.
    /// </summary>
    private static bool ShouldScan(string path)
        => SourcePath.From(path).IsCoreProject;

    /// <summary>
    /// Returns the CommandText token when the assignment target is that name.
    /// Use: High (every assignment). Scope: this analyzer.
    /// </summary>
    private static SyntaxToken? CommandTextToken(ExpressionSyntax left)
    {
        return left switch
        {
            MemberAccessExpressionSyntax member when member.Name.Identifier.ValueText == CommandTextName
                => member.Name.Identifier,
            MemberBindingExpressionSyntax binding when binding.Name.Identifier.ValueText == CommandTextName
                => binding.Name.Identifier,
            IdentifierNameSyntax identifier when identifier.Identifier.ValueText == CommandTextName
                => identifier.Identifier,
            ConditionalAccessExpressionSyntax conditional => CommandTextToken(conditional.WhenNotNull),
            _ => null,
        };
    }

    /// <summary>
    /// True when <paramref name="method"/> is a raw-SQL entry point on the EF Core relational surface.
    /// StoreObjectIdentifier.SqlQuery lives in the same assembly and is a mapping factory, so the
    /// containing type is part of the check.
    /// Use: High (every resolved call and method group). Scope: this analyzer.
    /// </summary>
    /// <param name="method">Resolved method, or null when the call does not bind.</param>
    private static bool IsEfRelationalRawSql(IMethodSymbol? method)
    {
        for (int depth = 0; method is not null && depth < MaxFlowDepth; depth++)
        {
            IMethodSymbol current = method.ReducedFrom ?? method;
            if (IsRawSqlDefinition(current.OriginalDefinition))
            {
                return true;
            }

            method = current.OverriddenMethod;
        }

        return false;
    }

    /// <summary>
    /// True when <paramref name="method"/> is declared on an EF Core relational raw-SQL type.
    /// Use: High (every candidate method). Scope: this analyzer.
    /// </summary>
    /// <param name="method">Original method definition.</param>
    private static bool IsRawSqlDefinition(ISymbol method)
    {
        if (!_rawSqlMethods.Contains(method.Name))
        {
            return false;
        }

        INamedTypeSymbol type = method.ContainingType;
        if (!string.Equals(type.ContainingAssembly.Name, RelationalAssemblyName, StringComparison.Ordinal))
        {
            return false;
        }

        string namespaceName = type.ContainingNamespace.ToDisplayString();
        return type.Name switch
        {
            "RelationalDatabaseFacadeExtensions" => namespaceName == EntityFrameworkNamespace,
            "RelationalQueryableExtensions" => namespaceName == EntityFrameworkNamespace,
            "MigrationBuilder" => namespaceName == MigrationsNamespace,
            _ => false,
        };
    }

    /// <summary>
    /// Returns the EF raw-SQL method name when <paramref name="operation"/> flows from one.
    /// Use: High (delegate invocations and method groups). Scope: this analyzer.
    /// </summary>
    /// <param name="operation">Value that may be a method group or a delegate alias.</param>
    /// <param name="state">Symbols already traced, and the semantic models available for this pass.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? RawSqlMethodName(
        IOperation? operation,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        if (operation is null || depth > MaxFlowDepth)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        switch (operation)
        {
            case IConversionOperation conversion:
                return RawSqlMethodName(conversion.Operand, state, depth + 1, cancellationToken);
            case IDelegateCreationOperation creation:
                return RawSqlMethodName(creation.Target, state, depth + 1, cancellationToken);
            case IMethodReferenceOperation methodReference when IsEfRelationalRawSql(methodReference.Method):
                return methodReference.Method.Name;
            case IParenthesizedOperation parenthesized:
                return RawSqlMethodName(parenthesized.Operand, state, depth + 1, cancellationToken);
            case IAssignmentOperation assignment:
                return RawSqlMethodName(assignment.Value, state, depth + 1, cancellationToken);
            default:
                return RawSqlBranchName(operation, state, depth, cancellationToken);
        }
    }

    /// <summary>
    /// Traces alternate values in their original evaluation order.
    /// Use: High (delegate alias tracing). Scope: conditional and coalescing operations.
    /// </summary>
    /// <param name="operation">Operation whose possible values are inspected.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? RawSqlBranchName(IOperation operation, FlowState state, int depth, CancellationToken cancellationToken)
    {
        switch (operation)
        {
            case IConditionalAccessOperation conditional:
                return RawSqlMethodName(conditional.WhenNotNull, state, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(conditional.Operation, state, depth + 1, cancellationToken);
            case IConditionalOperation conditional:
                return RawSqlMethodName(conditional.WhenTrue, state, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(conditional.WhenFalse, state, depth + 1, cancellationToken);
            case ICoalesceOperation coalesce:
                return RawSqlMethodName(coalesce.Value, state, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(coalesce.WhenNull, state, depth + 1, cancellationToken);
            default:
                return RawSqlReferenceName(operation, state, depth, cancellationToken);
        }
    }

    /// <summary>
    /// Traces reference and invocation operations after the value wrappers have been considered.
    /// Use: High (delegate alias tracing). Scope: this analyzer.
    /// </summary>
    /// <param name="operation">Reference or invocation to trace.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? RawSqlReferenceName(
        IOperation operation,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        switch (operation)
        {
            case ILocalReferenceOperation local:
                return SymbolFlowsFromRawSql(local.Local, state, depth + 1, cancellationToken);
            case IParameterReferenceOperation parameter:
                return SymbolFlowsFromRawSql(parameter.Parameter, state, depth + 1, cancellationToken);
            case IFieldReferenceOperation field:
                return SymbolFlowsFromRawSql(field.Field, state, depth + 1, cancellationToken);
            case IPropertyReferenceOperation property:
                return SymbolFlowsFromRawSql(property.Property, state, depth + 1, cancellationToken);
            case IInvocationOperation invocation when invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke:
                return RawSqlMethodName(invocation.Instance, state, depth + 1, cancellationToken);
            case IInvocationOperation invocation:
                return MethodReturnsRawSql(invocation.TargetMethod, state, depth + 1, cancellationToken);
            default:
                return null;
        }
    }

    /// <summary>
    /// Returns the EF method name when a stored delegate was filled from a raw-SQL method group.
    /// Use: High (delegate alias tracing). Scope: every part of the symbol's partial type.
    /// </summary>
    /// <param name="symbol">Local, parameter, field, or property that holds a delegate.</param>
    /// <param name="state">Symbols already traced, and the semantic models available for this pass.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? SymbolFlowsFromRawSql(
        ISymbol symbol,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        if (!state.Visited.Add(symbol))
        {
            return null;
        }

        // Initializers precede writes so the first match and visited-symbol state remain unchanged.
        string? name = InitializerFlowsFromRawSql(symbol, state, depth, cancellationToken)
            ?? AssignmentFlowsFromRawSql(symbol, state, depth, cancellationToken);
        if (name is not null)
        {
            return name;
        }

        if (symbol is IParameterSymbol parameter)
        {
            return ArgumentFlowsFromRawSql(parameter, state, depth, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Traces delegate initializers in declaration order.
    /// Use: Medium (delegate alias tracing). Scope: the symbol declarations.
    /// </summary>
    /// <param name="symbol">Delegate symbol to trace.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? InitializerFlowsFromRawSql(ISymbol symbol, FlowState state, int depth, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = reference.GetSyntax(cancellationToken);
            ExpressionSyntax? initializer = syntax switch
            {
                VariableDeclaratorSyntax declarator => declarator.Initializer?.Value,
                PropertyDeclarationSyntax property => property.Initializer?.Value,
                _ => null,
            };
            string? name = ValueFlowsFromRawSql(initializer, state, depth, cancellationToken);
            if (name is not null)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Traces assignments to a delegate symbol in scope order.
    /// Use: Medium (delegate alias tracing). Scope: the symbol assignment scopes.
    /// </summary>
    /// <param name="symbol">Delegate symbol to trace.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? AssignmentFlowsFromRawSql(ISymbol symbol, FlowState state, int depth, CancellationToken cancellationToken)
    {
        foreach (SyntaxNode scope in AssignmentScopes(symbol, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SemanticModel? scopeModel = state.ModelFor(scope.SyntaxTree);
            if (scopeModel is null)
            {
                continue;
            }

            foreach (AssignmentExpressionSyntax assignment in scope.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (!IsSymbolReference(scopeModel.GetOperation(assignment.Left, cancellationToken), symbol))
                {
                    continue;
                }

                string? name = ValueFlowsFromRawSql(assignment.Right, state, depth, cancellationToken);
                if (name is not null)
                {
                    return name;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the EF method name when a call passes a raw-SQL delegate into <paramref name="parameter"/>.
    /// Use: Medium (delegate parameter tracing). Scope: every part of the parameter's partial type.
    /// </summary>
    /// <param name="parameter">Delegate parameter to trace back to its arguments.</param>
    /// <param name="state">Symbols already traced, and the semantic models available for this pass.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? ArgumentFlowsFromRawSql(
        IParameterSymbol parameter,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        if (parameter.ContainingSymbol is not IMethodSymbol method)
        {
            return null;
        }

        foreach (SyntaxNode scope in TypeScopes(parameter, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            SemanticModel? scopeModel = state.ModelFor(scope.SyntaxTree);
            if (scopeModel is null)
            {
                continue;
            }

            foreach (InvocationExpressionSyntax invocation in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (scopeModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation call
                    || !SymbolEqualityComparer.Default.Equals(call.TargetMethod.OriginalDefinition, method.OriginalDefinition))
                {
                    continue;
                }

                string? name = CallArgumentFlowsFromRawSql(call, parameter, state, depth, cancellationToken);
                if (name is not null)
                {
                    return name;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Traces the matching arguments of a call in order.
    /// Use: Medium (delegate alias tracing). Scope: one invocation.
    /// </summary>
    /// <param name="call">Invocation whose arguments are inspected.</param>
    /// <param name="parameter">Delegate parameter to match.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? CallArgumentFlowsFromRawSql(
        IInvocationOperation call, IParameterSymbol parameter, FlowState state, int depth, CancellationToken cancellationToken)
    {
        foreach (IArgumentOperation argument in call.Arguments)
        {
            if (!SymbolEqualityComparer.Default.Equals(argument.Parameter?.OriginalDefinition, parameter.OriginalDefinition))
            {
                continue;
            }

            string? name = RawSqlMethodName(argument.Value, state, depth + 1, cancellationToken);
            if (name is not null)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the EF method name when <paramref name="method"/> returns a raw-SQL delegate.
    /// Use: Medium (returned delegate aliases). Scope: every declaration of the method.
    /// </summary>
    /// <param name="method">Method whose returned delegate may be invoked.</param>
    /// <param name="state">Symbols already traced, and the semantic models available for this pass.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? MethodReturnsRawSql(
        IMethodSymbol method,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        method = method.OriginalDefinition;
        if (!state.Visited.Add(method))
        {
            return null;
        }

        foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
        {
            string? name = DeclarationReturnsRawSql(reference.GetSyntax(cancellationToken), state, depth, cancellationToken);
            if (name is not null)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Traces the expression body and return statements of a declaration.
    /// Use: Medium (delegate alias tracing). Scope: one method declaration.
    /// </summary>
    /// <param name="syntax">Method declaration to inspect.</param>
    /// <param name="state">Symbols already traced, and available semantic models.</param>
    /// <param name="depth">Current alias depth.</param>
    /// <param name="cancellationToken">Cancellation token for semantic lookups.</param>
    /// <returns>The raw-SQL method name, or null when none is found.</returns>
    private static string? DeclarationReturnsRawSql(SyntaxNode syntax, FlowState state, int depth, CancellationToken cancellationToken)
    {
        ExpressionSyntax? expressionBody = MethodExpressionBody(syntax);
        string? name = ValueFlowsFromRawSql(expressionBody, state, depth, cancellationToken);
        if (name is not null)
        {
            return name;
        }

        foreach (ReturnStatementSyntax returnStatement in syntax.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression is null || !BelongsToMethod(returnStatement, syntax))
            {
                continue;
            }

            name = ValueFlowsFromRawSql(returnStatement.Expression, state, depth, cancellationToken);
            if (name is not null)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the expression body of a method declaration, local function, or accessor.
    /// Use: Medium (return tracing). Scope: one method declaration.
    /// </summary>
    /// <param name="syntax">Declaration to inspect.</param>
    /// <returns>The expression body, or null when none is present.</returns>
    private static ExpressionSyntax? MethodExpressionBody(SyntaxNode syntax)
        => syntax switch
        {
            MethodDeclarationSyntax declaration => declaration.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax localFunction => localFunction.ExpressionBody?.Expression,
            AccessorDeclarationSyntax accessor => accessor.ExpressionBody?.Expression,
            _ => null,
        };

    /// <summary>
    /// True when <paramref name="returnStatement"/> returns from <paramref name="methodSyntax"/> itself.
    /// Use: Medium (return tracing). Scope: one method declaration.
    /// </summary>
    /// <param name="returnStatement">Return statement found under the method.</param>
    /// <param name="methodSyntax">Syntax that declares the method being traced.</param>
    private static bool BelongsToMethod(SyntaxNode returnStatement, SyntaxNode methodSyntax)
    {
        LocalFunctionStatementSyntax? localFunction = returnStatement.Ancestors().OfType<LocalFunctionStatementSyntax>().FirstOrDefault();
        if (localFunction is not null && localFunction != methodSyntax)
        {
            return false;
        }

        return returnStatement.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault() is null;
    }

    /// <summary>
    /// Returns the EF method name when <paramref name="expression"/> flows from a raw-SQL method group.
    /// Use: High (initializers, assignments, and returns). Scope: this analyzer.
    /// </summary>
    /// <param name="expression">Expression to evaluate, or null.</param>
    /// <param name="state">Symbols already traced, and the semantic models available for this pass.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for semantic lookups.</param>
    private static string? ValueFlowsFromRawSql(
        SyntaxNode? expression,
        FlowState state,
        int depth,
        CancellationToken cancellationToken)
    {
        if (expression is null)
        {
            return null;
        }

        SemanticModel? model = state.ModelFor(expression.SyntaxTree);
        if (model is null)
        {
            return null;
        }

        return RawSqlMethodName(model.GetOperation(expression, cancellationToken), state, depth, cancellationToken);
    }

    /// <summary>
    /// Declarations of the type that contains <paramref name="symbol"/>.
    /// Use: Medium (delegate argument tracing). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Symbol whose containing type is searched.</param>
    /// <param name="cancellationToken">Cancellation token for syntax lookups.</param>
    private static IEnumerable<SyntaxNode> TypeScopes(ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.ContainingType is not { } type)
        {
            yield break;
        }

        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            yield return reference.GetSyntax(cancellationToken);
        }
    }

    /// <summary>
    /// Syntax that can assign <paramref name="symbol"/>: its method for a local, its type otherwise.
    /// Use: Medium (alias tracing). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Symbol whose writes are in scope.</param>
    /// <param name="cancellationToken">Cancellation token for syntax lookups.</param>
    private static IEnumerable<SyntaxNode> AssignmentScopes(ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol is ILocalSymbol or IParameterSymbol)
        {
            SyntaxNode? scope = LocalAssignmentScope(symbol, cancellationToken);

            if (scope is not null)
            {
                yield return scope;
            }

            yield break;
        }

        foreach (SyntaxNode scope in TypeScopes(symbol, cancellationToken))
        {
            yield return scope;
        }
    }

    /// <summary>
    /// Finds the assignment scope for a local or parameter, including its enclosing method when nested.
    /// Use: Medium (alias tracing). Scope: one symbol declaration.
    /// </summary>
    /// <param name="symbol">Local or parameter whose writes are searched.</param>
    /// <param name="cancellationToken">Cancellation token for syntax lookups.</param>
    /// <returns>The assignment scope, or null when no declaration is available.</returns>
    private static SyntaxNode? LocalAssignmentScope(ISymbol symbol, CancellationToken cancellationToken)
    {
        SyntaxNode? scope = symbol.DeclaringSyntaxReferences
            .FirstOrDefault()
            ?.GetSyntax(cancellationToken)
            .AncestorsAndSelf()
            .FirstOrDefault(IsAssignmentScope);
        if (scope is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
        {
            SyntaxNode? outer = scope.Ancestors().FirstOrDefault(static node => node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax);
            scope = outer ?? scope;
        }

        return scope;
    }

    /// <summary>
    /// Identifies a syntax scope that can contain local or parameter assignments.
    /// Use: Medium (alias tracing). Scope: ancestors of a symbol declaration.
    /// </summary>
    /// <param name="node">Ancestor to inspect.</param>
    /// <returns>True when the node is an assignment scope.</returns>
    private static bool IsAssignmentScope(SyntaxNode node)
        => node switch
        {
            BaseMethodDeclarationSyntax => true,
            LocalFunctionStatementSyntax => true,
            AnonymousFunctionExpressionSyntax => true,
            AccessorDeclarationSyntax => true,
            CompilationUnitSyntax => true,
            _ => false,
        };

    /// <summary>
    /// True when <paramref name="operation"/> refers to <paramref name="symbol"/>.
    /// Use: Medium (assignment tracing). Scope: this analyzer.
    /// </summary>
    /// <param name="operation">Operation on the left of an assignment.</param>
    /// <param name="symbol">Symbol expected on that left side.</param>
    private static bool IsSymbolReference(IOperation? operation, ISymbol symbol)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is IParenthesizedOperation parenthesized)
        {
            return IsSymbolReference(parenthesized.Operand, symbol);
        }

        return operation switch
        {
            ILocalReferenceOperation local => SymbolEqualityComparer.Default.Equals(local.Local, symbol),
            IParameterReferenceOperation parameter => SymbolEqualityComparer.Default.Equals(parameter.Parameter, symbol),
            IFieldReferenceOperation field => SymbolEqualityComparer.Default.Equals(field.Field, symbol),
            IPropertyReferenceOperation property => SymbolEqualityComparer.Default.Equals(property.Property, symbol),
            _ => false,
        };
    }

    /// <summary>
    /// Unwraps a method-group operand to the method reference.
    /// Use: High (delegate creation and conversions). Scope: this analyzer.
    /// </summary>
    /// <param name="operation">Delegate target or conversion operand.</param>
    private static IMethodReferenceOperation? MethodGroup(IOperation operation)
    {
        IOperation current = operation;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        return current as IMethodReferenceOperation;
    }

    /// <summary>
    /// Reports CB1003 at <paramref name="location"/>.
    /// Use: High (every raw-SQL hit). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Syntax analysis context.</param>
    /// <param name="location">Source location to highlight.</param>
    /// <param name="name">Raw-SQL method name for the diagnostic message.</param>
    private static void Report(SyntaxNodeAnalysisContext context, Location location, string name)
        => context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ScatteredSql,
            location,
            name));

    /// <summary>
    /// Reports CB1003 at <paramref name="location"/>.
    /// Use: High (every raw-SQL method group). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Operation analysis context.</param>
    /// <param name="location">Source location to highlight.</param>
    /// <param name="name">Raw-SQL method name for the diagnostic message.</param>
    private static void Report(OperationAnalysisContext context, Location location, string name)
        => context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ScatteredSql,
            location,
            name));

    /// <summary>
    /// Keeps the semantic model supplied for one tree. Compilation.GetSemanticModel is not used.
    /// Use: High (every tree). Scope: this compilation.
    /// </summary>
    /// <param name="models">Models collected for the compilation.</param>
    /// <param name="model">Semantic model from the tree's own analysis callback.</param>
    private static void Remember(ConcurrentDictionary<SyntaxTree, SemanticModel> models, SemanticModel model)
        => models.TryAdd(model.SyntaxTree, model);

    /// <summary>
    /// Reports delegate invocations whose raw-SQL alias is written in another part of a partial class.
    /// Use: High (compilation end). Scope: invocations deferred from the inline pass.
    /// </summary>
    /// <param name="context">Compilation end context.</param>
    /// <param name="models">Semantic model of every tree in the compilation.</param>
    /// <param name="deferred">Invocations the inline pass could not finish.</param>
    private static void ReportDeferred(
        CompilationAnalysisContext context,
        ConcurrentDictionary<SyntaxTree, SemanticModel> models,
        ConcurrentBag<DeferredInvoke> deferred)
    {
        foreach (DeferredInvoke item in deferred)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            string? name = RawSqlMethodName(
                item.Instance,
                new FlowState(models),
                depth: 0,
                context.CancellationToken);
            if (name is null)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                CipherBankDiagnostics.ScatteredSql,
                item.Location,
                name));
        }
    }

    /// <summary>
    /// An invocation whose delegate target may be assigned in another partial-class part.
    /// </summary>
    private readonly struct DeferredInvoke : IEquatable<DeferredInvoke>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredInvoke"/> struct.
        /// </summary>
        /// <param name="instance">Delegate instance that was not fully traced.</param>
        /// <param name="location">Invocation location to report when the alias is raw SQL.</param>
        public DeferredInvoke(IOperation instance, Location location)
        {
            Instance = instance;
            Location = location;
        }

        /// <summary>Gets the delegate instance that was not fully traced.</summary>
        public IOperation Instance { get; }

        /// <summary>Gets the invocation location to report when the alias is raw SQL.</summary>
        public Location Location { get; }

        /// <summary>Compares two deferred invocations for equality.</summary>
        /// <param name="left">First invocation.</param>
        /// <param name="right">Second invocation.</param>
        /// <returns>True when both stored values are equal.</returns>
        public static bool operator ==(DeferredInvoke left, DeferredInvoke right) => left.Equals(right);

        /// <summary>Compares two deferred invocations for inequality.</summary>
        /// <param name="left">First invocation.</param>
        /// <param name="right">Second invocation.</param>
        /// <returns>True when either stored value differs.</returns>
        public static bool operator !=(DeferredInvoke left, DeferredInvoke right) => !left.Equals(right);

        /// <inheritdoc />
        public bool Equals(DeferredInvoke other)
            => EqualityComparer<IOperation>.Default.Equals(Instance, other.Instance)
                && EqualityComparer<Location>.Default.Equals(Location, other.Location);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DeferredInvoke other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Instance is null ? 0 : EqualityComparer<IOperation>.Default.GetHashCode(Instance);
                return (hash * 31) + (Location is null ? 0 : EqualityComparer<Location>.Default.GetHashCode(Location));
            }
        }
    }

    /// <summary>
    /// Alias trace for one invocation. A single-tree pass records missing partial-class parts as incomplete.
    /// The compilation-end pass supplies a semantic model for every part.
    /// </summary>
    private sealed class FlowState
    {
        private readonly SemanticModel? _current;
        private readonly ConcurrentDictionary<SyntaxTree, SemanticModel>? _models;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowState"/> class for the tree being analyzed.
        /// </summary>
        /// <param name="current">Semantic model of the invocation's tree.</param>
        public FlowState(SemanticModel current)
        {
            _current = current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlowState"/> class for every tree in the compilation.
        /// </summary>
        /// <param name="models">Semantic model of every tree, keyed by that tree.</param>
        public FlowState(ConcurrentDictionary<SyntaxTree, SemanticModel> models)
        {
            _models = models;
        }

        /// <summary>Gets symbols already traced, so aliases cannot loop.</summary>
        public HashSet<ISymbol> Visited { get; } = new(SymbolEqualityComparer.Default);

        /// <summary>Gets a value indicating whether a partial-class part had no semantic model yet.</summary>
        public bool Incomplete { get; private set; }

        /// <summary>
        /// Returns the semantic model for <paramref name="tree"/>, or null when this pass cannot bind it.
        /// </summary>
        /// <param name="tree">Tree that contains the syntax being traced.</param>
        /// <returns>The semantic model for <paramref name="tree"/>, or null when it is not available yet.</returns>
        public SemanticModel? ModelFor(SyntaxTree tree)
        {
            if (_models is not null)
            {
                if (_models.TryGetValue(tree, out SemanticModel? model))
                {
                    return model;
                }

                Incomplete = true;
                return null;
            }

            if (_current is not null && _current.SyntaxTree == tree)
            {
                return _current;
            }

            Incomplete = true;
            return null;
        }
    }
}
