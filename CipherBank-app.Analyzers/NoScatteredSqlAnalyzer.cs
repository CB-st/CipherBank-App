// <copyright file="NoScatteredSqlAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags raw SQL in CipherBank-app.Core.
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
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Roslyn analysis context for this compilation.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterOperationAction(AnalyzeDelegateCreation, OperationKind.DelegateCreation);
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
    /// Use: High (every invocation). Scope: Core C# trees.
    /// </summary>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
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

        string? name = RawSqlMethodName(
            invocation.Instance,
            context.SemanticModel,
            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
            depth: 0,
            context.CancellationToken);
        if (name is null)
        {
            return;
        }

        Report(context, invocation.Syntax.GetLocation(), name);
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
    private static bool IsRawSqlDefinition(IMethodSymbol method)
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
    /// <param name="model">Semantic model for the tree that contains <paramref name="operation"/>.</param>
    /// <param name="visited">Symbols already traced, so aliases cannot loop.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? RawSqlMethodName(
        IOperation? operation,
        SemanticModel model,
        HashSet<ISymbol> visited,
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
                return RawSqlMethodName(conversion.Operand, model, visited, depth + 1, cancellationToken);
            case IDelegateCreationOperation creation:
                return RawSqlMethodName(creation.Target, model, visited, depth + 1, cancellationToken);
            case IMethodReferenceOperation methodReference when IsEfRelationalRawSql(methodReference.Method):
                return methodReference.Method.Name;
            case IParenthesizedOperation parenthesized:
                return RawSqlMethodName(parenthesized.Operand, model, visited, depth + 1, cancellationToken);
            case IConditionalAccessOperation conditional:
                return RawSqlMethodName(conditional.WhenNotNull, model, visited, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(conditional.Operation, model, visited, depth + 1, cancellationToken);
            case IConditionalOperation conditional:
                return RawSqlMethodName(conditional.WhenTrue, model, visited, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(conditional.WhenFalse, model, visited, depth + 1, cancellationToken);
            case ICoalesceOperation coalesce:
                return RawSqlMethodName(coalesce.Value, model, visited, depth + 1, cancellationToken)
                    ?? RawSqlMethodName(coalesce.WhenNull, model, visited, depth + 1, cancellationToken);
            case IAssignmentOperation assignment:
                return RawSqlMethodName(assignment.Value, model, visited, depth + 1, cancellationToken);
            case ILocalReferenceOperation local:
                return SymbolFlowsFromRawSql(local.Local, model, visited, depth + 1, cancellationToken);
            case IParameterReferenceOperation parameter:
                return SymbolFlowsFromRawSql(parameter.Parameter, model, visited, depth + 1, cancellationToken);
            case IFieldReferenceOperation field:
                return SymbolFlowsFromRawSql(field.Field, model, visited, depth + 1, cancellationToken);
            case IPropertyReferenceOperation property:
                return SymbolFlowsFromRawSql(property.Property, model, visited, depth + 1, cancellationToken);
            case IInvocationOperation invocation when invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke:
                return RawSqlMethodName(invocation.Instance, model, visited, depth + 1, cancellationToken);
            case IInvocationOperation invocation:
                return MethodReturnsRawSql(invocation.TargetMethod, model, visited, depth + 1, cancellationToken);
            default:
                return null;
        }
    }

    /// <summary>
    /// Returns the EF method name when a stored delegate was filled from a raw-SQL method group.
    /// Use: High (delegate alias tracing). Scope: this analyzer.
    /// </summary>
    /// <param name="symbol">Local, parameter, field, or property that holds a delegate.</param>
    /// <param name="model">Semantic model for the tree being analyzed.</param>
    /// <param name="visited">Symbols already traced.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? SymbolFlowsFromRawSql(
        ISymbol symbol,
        SemanticModel model,
        HashSet<ISymbol> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(symbol))
        {
            return null;
        }

        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = reference.GetSyntax(cancellationToken);
            ExpressionSyntax? initializer = syntax switch
            {
                VariableDeclaratorSyntax declarator => declarator.Initializer?.Value,
                PropertyDeclarationSyntax property => property.Initializer?.Value,
                _ => null,
            };
            string? name = ValueFlowsFromRawSql(initializer, model, visited, depth, cancellationToken);
            if (name is not null)
            {
                return name;
            }
        }

        foreach (SyntaxNode scope in AssignmentScopes(symbol, cancellationToken))
        {
            if (scope.SyntaxTree != model.SyntaxTree)
            {
                continue;
            }

            foreach (AssignmentExpressionSyntax assignment in scope.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (!IsSymbolReference(model.GetOperation(assignment.Left, cancellationToken), symbol))
                {
                    continue;
                }

                string? name = ValueFlowsFromRawSql(assignment.Right, model, visited, depth, cancellationToken);
                if (name is not null)
                {
                    return name;
                }
            }
        }

        if (symbol is IParameterSymbol parameter)
        {
            return ArgumentFlowsFromRawSql(parameter, model, visited, depth, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Returns the EF method name when a call passes a raw-SQL delegate into <paramref name="parameter"/>.
    /// Use: Medium (delegate parameter tracing). Scope: the parameter's containing type.
    /// </summary>
    /// <param name="parameter">Delegate parameter to trace back to its arguments.</param>
    /// <param name="model">Semantic model for the tree being analyzed.</param>
    /// <param name="visited">Symbols already traced.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? ArgumentFlowsFromRawSql(
        IParameterSymbol parameter,
        SemanticModel model,
        HashSet<ISymbol> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        if (parameter.ContainingSymbol is not IMethodSymbol method)
        {
            return null;
        }

        foreach (SyntaxNode scope in TypeScopes(parameter, cancellationToken))
        {
            if (scope.SyntaxTree != model.SyntaxTree)
            {
                continue;
            }

            foreach (InvocationExpressionSyntax invocation in scope.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetOperation(invocation, cancellationToken) is not IInvocationOperation call
                    || !SymbolEqualityComparer.Default.Equals(call.TargetMethod.OriginalDefinition, method.OriginalDefinition))
                {
                    continue;
                }

                foreach (IArgumentOperation argument in call.Arguments)
                {
                    if (!SymbolEqualityComparer.Default.Equals(argument.Parameter, parameter))
                    {
                        continue;
                    }

                    string? name = RawSqlMethodName(argument.Value, model, visited, depth + 1, cancellationToken);
                    if (name is not null)
                    {
                        return name;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the EF method name when <paramref name="method"/> returns a raw-SQL delegate.
    /// Use: Medium (returned delegate aliases). Scope: the method body.
    /// </summary>
    /// <param name="method">Method whose returned delegate may be invoked.</param>
    /// <param name="model">Semantic model for the tree being analyzed.</param>
    /// <param name="visited">Symbols already traced.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for syntax and semantic lookups.</param>
    private static string? MethodReturnsRawSql(
        IMethodSymbol method,
        SemanticModel model,
        HashSet<ISymbol> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        method = method.OriginalDefinition;
        if (!visited.Add(method))
        {
            return null;
        }

        foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = reference.GetSyntax(cancellationToken);
            ExpressionSyntax? expressionBody = syntax switch
            {
                MethodDeclarationSyntax declaration => declaration.ExpressionBody?.Expression,
                LocalFunctionStatementSyntax localFunction => localFunction.ExpressionBody?.Expression,
                AccessorDeclarationSyntax accessor => accessor.ExpressionBody?.Expression,
                _ => null,
            };
            string? name = ValueFlowsFromRawSql(expressionBody, model, visited, depth, cancellationToken);
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

                name = ValueFlowsFromRawSql(returnStatement.Expression, model, visited, depth, cancellationToken);
                if (name is not null)
                {
                    return name;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// True when <paramref name="returnStatement"/> returns from <paramref name="methodSyntax"/> itself.
    /// Use: Medium (return tracing). Scope: one method declaration.
    /// </summary>
    /// <param name="returnStatement">Return statement found under the method.</param>
    /// <param name="methodSyntax">Syntax that declares the method being traced.</param>
    private static bool BelongsToMethod(ReturnStatementSyntax returnStatement, SyntaxNode methodSyntax)
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
    /// <param name="model">Semantic model for the tree being analyzed.</param>
    /// <param name="visited">Symbols already traced.</param>
    /// <param name="depth">Remaining alias hops.</param>
    /// <param name="cancellationToken">Cancellation token for semantic lookups.</param>
    private static string? ValueFlowsFromRawSql(
        ExpressionSyntax? expression,
        SemanticModel model,
        HashSet<ISymbol> visited,
        int depth,
        CancellationToken cancellationToken)
    {
        if (expression is null || expression.SyntaxTree != model.SyntaxTree)
        {
            return null;
        }

        return RawSqlMethodName(model.GetOperation(expression, cancellationToken), model, visited, depth, cancellationToken);
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
            SyntaxNode? scope = symbol.DeclaringSyntaxReferences
                .FirstOrDefault()
                ?.GetSyntax(cancellationToken)
                .AncestorsAndSelf()
                .FirstOrDefault(static node => node is
                    BaseMethodDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or AnonymousFunctionExpressionSyntax
                    or AccessorDeclarationSyntax
                    or CompilationUnitSyntax);
            if (scope is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
            {
                SyntaxNode? outer = scope.Ancestors().FirstOrDefault(static node => node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax);
                scope = outer ?? scope;
            }

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
}
