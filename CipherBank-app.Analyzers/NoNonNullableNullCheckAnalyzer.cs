// <copyright file="NoNonNullableNullCheckAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Reports pure null checks performed on non-nullable value or reference types.
/// Use: High (every compilation). Scope: C# syntax trees with semantic nullability context.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoNonNullableNullCheckAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [CipherBankDiagnostics.NonNullableNullCheck];

    /// <summary>
    /// Registers compilation-scoped syntax actions for null-check constructs.
    /// Use: High (every compilation). Scope: this analyzer.
    /// </summary>
    /// <param name="context">Analyzer initialization context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterCompilationActions);
    }

    private static void RegisterCompilationActions(CompilationStartAnalysisContext context)
    {
        ISymbol? argumentNullExceptionType =
            context.Compilation.GetTypeByMetadataName("System.ArgumentNullException");

        context.RegisterSyntaxNodeAction(
            nodeContext => AnalyzeInvocation(nodeContext, argumentNullExceptionType),
            SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeEquality,
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeIsPattern,
            SyntaxKind.IsPatternExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeCoalesce,
            SyntaxKind.CoalesceExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeCoalesceAssignment,
            SyntaxKind.CoalesceAssignmentExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeConditionalAccess,
            SyntaxKind.ConditionalAccessExpression);
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        ISymbol? argumentNullExceptionType)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        string? methodName = NullCheckSyntax.GetInvokedMethodName(invocation.Expression);
        if (methodName is not ("ThrowIfNull" or "ReferenceEquals" or "Equals"))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        ExpressionSyntax? target = NullCheckSyntax.GetInvocationCheckedOperand(
            invocation,
            method,
            argumentNullExceptionType);
        if (target is not null)
        {
            ReportIfNonNullable(context, target, invocation.GetLocation(), skipConditionalAccessOperand: true);
        }
    }

    private static void AnalyzeEquality(SyntaxNodeAnalysisContext context)
    {
        BinaryExpressionSyntax binary = (BinaryExpressionSyntax)context.Node;
        ExpressionSyntax? target = NullCheckSyntax.GetNullComparisonOperand(binary);
        if (target is null
            || context.SemanticModel.GetSymbolInfo(binary, context.CancellationToken).Symbol
                is not IMethodSymbol { MethodKind: MethodKind.BuiltinOperator })
        {
            return;
        }

        ReportIfNonNullable(context, target, binary.GetLocation(), skipConditionalAccessOperand: true);
    }

    private static void AnalyzeIsPattern(SyntaxNodeAnalysisContext context)
    {
        IsPatternExpressionSyntax isPattern = (IsPatternExpressionSyntax)context.Node;
        if (NullCheckSyntax.IsPureNullPattern(isPattern.Pattern))
        {
            ReportIfNonNullable(
                context,
                isPattern.Expression,
                isPattern.GetLocation(),
                skipConditionalAccessOperand: true);
        }
    }

    private static void AnalyzeCoalesce(SyntaxNodeAnalysisContext context)
    {
        BinaryExpressionSyntax binary = (BinaryExpressionSyntax)context.Node;
        ReportIfNonNullable(context, binary.Left, binary.GetLocation(), skipConditionalAccessOperand: true);
    }

    private static void AnalyzeCoalesceAssignment(SyntaxNodeAnalysisContext context)
    {
        AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)context.Node;
        ReportIfNonNullable(context, assignment.Left, assignment.GetLocation(), skipConditionalAccessOperand: false);
    }

    private static void AnalyzeConditionalAccess(SyntaxNodeAnalysisContext context)
    {
        ConditionalAccessExpressionSyntax conditionalAccess = (ConditionalAccessExpressionSyntax)context.Node;
        ReportIfNonNullable(
            context,
            conditionalAccess.Expression,
            conditionalAccess.GetLocation(),
            skipConditionalAccessOperand: false);
    }

    private static void ReportIfNonNullable(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax expression,
        Location diagnosticLocation,
        bool skipConditionalAccessOperand)
    {
        ExpressionSyntax unwrapped = NullCheckSyntax.UnwrapParentheses(expression);
        if (skipConditionalAccessOperand && unwrapped is ConditionalAccessExpressionSyntax)
        {
            return;
        }

        string? typeName = NonNullableTypeClassifier.TryGetNonNullableTypeName(
            unwrapped,
            context.SemanticModel,
            context.CancellationToken);
        if (typeName is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CipherBankDiagnostics.NonNullableNullCheck,
                diagnosticLocation,
                typeName));
        }
    }
}
