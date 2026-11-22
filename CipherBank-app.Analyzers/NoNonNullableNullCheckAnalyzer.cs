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
        INamedTypeSymbol? argumentNullExceptionType =
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
        INamedTypeSymbol? argumentNullExceptionType)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        string? methodName = GetInvokedMethodName(invocation.Expression);
        if (methodName is not ("ThrowIfNull" or "ReferenceEquals" or "Equals"))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method)
        {
            return;
        }

        ExpressionSyntax? target = GetInvocationCheckedOperand(
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
        ExpressionSyntax? target = GetNullComparisonOperand(binary);
        if (target is not null)
        {
            ReportIfNonNullable(context, target, binary.GetLocation(), skipConditionalAccessOperand: true);
        }
    }

    private static void AnalyzeIsPattern(SyntaxNodeAnalysisContext context)
    {
        IsPatternExpressionSyntax isPattern = (IsPatternExpressionSyntax)context.Node;
        if (IsPureNullPattern(isPattern.Pattern))
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
        ExpressionSyntax unwrapped = UnwrapParentheses(expression);
        if (skipConditionalAccessOperand && unwrapped is ConditionalAccessExpressionSyntax)
        {
            return;
        }

        string? typeName = TryGetNonNullableTypeName(
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

    private static string? TryGetNonNullableTypeName(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
        ITypeSymbol? type = typeInfo.Type;
        if (type is null || IsUnsupportedTypeKind(type))
        {
            return null;
        }

        if (type.IsValueType)
        {
            return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? null
                : type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        if (!IsNonNullableReferenceOrTypeParameter(
                expression,
                semanticModel,
                typeInfo,
                type,
                cancellationToken))
        {
            return null;
        }

        return type
            .WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            .ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    private static bool IsUnsupportedTypeKind(ITypeSymbol type)
        => type is IErrorTypeSymbol
            || type.TypeKind is TypeKind.Dynamic or TypeKind.Pointer or TypeKind.FunctionPointer;

    private static bool IsNonNullableReferenceOrTypeParameter(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        TypeInfo typeInfo,
        ITypeSymbol type,
        CancellationToken cancellationToken)
    {
        if (!semanticModel.GetNullableContext(expression.SpanStart).AnnotationsEnabled())
        {
            return false;
        }

        NullableAnnotation annotation = GetDeclaredNullableAnnotation(
            expression,
            semanticModel,
            typeInfo,
            type,
            cancellationToken);
        if (annotation != NullableAnnotation.NotAnnotated)
        {
            return false;
        }

        return type is ITypeParameterSymbol typeParameter
            ? IsConstrainedNonNullable(typeParameter)
            : type.IsReferenceType;
    }

    private static NullableAnnotation GetDeclaredNullableAnnotation(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        TypeInfo typeInfo,
        ITypeSymbol type,
        CancellationToken cancellationToken)
    {
        if (TryGetSymbolNullableAnnotation(
                expression,
                semanticModel,
                cancellationToken,
                out NullableAnnotation symbolAnnotation))
        {
            return symbolAnnotation;
        }

        if (expression is ElementAccessExpressionSyntax elementAccess
            && semanticModel.GetTypeInfo(elementAccess.Expression, cancellationToken).Type
                is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementNullableAnnotation;
        }

        return typeInfo.Nullability.Annotation != NullableAnnotation.None
            ? typeInfo.Nullability.Annotation
            : type.NullableAnnotation;
    }

    private static bool TryGetSymbolNullableAnnotation(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out NullableAnnotation annotation)
    {
        ISymbol? symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        switch (symbol)
        {
            case ILocalSymbol local:
                annotation = local.NullableAnnotation;
                return true;
            case IParameterSymbol parameter:
                annotation = parameter.NullableAnnotation;
                return true;
            case IFieldSymbol field:
                annotation = field.NullableAnnotation;
                return true;
            case IPropertySymbol property:
                annotation = property.NullableAnnotation;
                return true;
            case IEventSymbol @event:
                annotation = @event.NullableAnnotation;
                return true;
            case IMethodSymbol { MethodKind: MethodKind.Constructor }:
                annotation = NullableAnnotation.NotAnnotated;
                return true;
            case IMethodSymbol method:
                annotation = GetMethodReturnAnnotation(method);
                return true;
            default:
                annotation = NullableAnnotation.None;
                return false;
        }
    }

    private static NullableAnnotation GetMethodReturnAnnotation(IMethodSymbol method)
        => method.ReturnNullableAnnotation == NullableAnnotation.NotAnnotated
            && HasMaybeNullReturnAttribute(method)
            ? NullableAnnotation.Annotated
            : method.ReturnNullableAnnotation;

    private static bool HasMaybeNullReturnAttribute(IMethodSymbol method)
    {
        foreach (AttributeData attribute in method.GetReturnTypeAttributes())
        {
            if (attribute.AttributeClass is
                {
                    Name: "MaybeNullAttribute",
                    ContainingNamespace:
                    {
                        Name: "CodeAnalysis",
                        ContainingNamespace:
                        {
                            Name: "Diagnostics",
                            ContainingNamespace:
                            {
                                Name: "System",
                                ContainingNamespace.IsGlobalNamespace: true,
                            },
                        },
                    },
                })
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsConstrainedNonNullable(ITypeParameterSymbol typeParameter)
    {
        if (typeParameter.HasValueTypeConstraint || typeParameter.HasNotNullConstraint)
        {
            return true;
        }

        if (typeParameter.HasReferenceTypeConstraint
            && typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.NotAnnotated)
        {
            return true;
        }

        for (int index = 0; index < typeParameter.ConstraintTypes.Length; index++)
        {
            if (typeParameter.ConstraintTypes[index].IsReferenceType
                && index < typeParameter.ConstraintNullableAnnotations.Length
                && typeParameter.ConstraintNullableAnnotations[index] == NullableAnnotation.NotAnnotated)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetInvokedMethodName(ExpressionSyntax expression)
        => expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
            _ => null,
        };

    private static ExpressionSyntax? GetInvocationCheckedOperand(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        INamedTypeSymbol? argumentNullExceptionType)
    {
        if (method.Name == "ThrowIfNull"
            && argumentNullExceptionType is not null
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, argumentNullExceptionType))
        {
            return FindArgumentExpression(invocation.ArgumentList.Arguments, "argument");
        }

        if (method.Name is "ReferenceEquals" or "Equals"
            && method.IsStatic
            && method.ContainingType?.SpecialType == SpecialType.System_Object
            && invocation.ArgumentList.Arguments.Count == 2)
        {
            return GetNullPairOperand(
                invocation.ArgumentList.Arguments[0].Expression,
                invocation.ArgumentList.Arguments[1].Expression);
        }

        return null;
    }

    private static ExpressionSyntax? FindArgumentExpression(
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        string parameterName)
    {
        foreach (ArgumentSyntax argument in arguments)
        {
            if (argument.NameColon?.Name.Identifier.ValueText == parameterName)
            {
                return argument.Expression;
            }
        }

        return arguments.Count > 0 && arguments[0].NameColon is null
            ? arguments[0].Expression
            : null;
    }

    private static ExpressionSyntax? GetNullComparisonOperand(BinaryExpressionSyntax binary)
        => GetNullPairOperand(binary.Left, binary.Right);

    private static ExpressionSyntax? GetNullPairOperand(ExpressionSyntax left, ExpressionSyntax right)
    {
        bool leftIsNull = IsNullLiteral(left);
        bool rightIsNull = IsNullLiteral(right);
        if (leftIsNull == rightIsNull)
        {
            return null;
        }

        return leftIsNull ? right : left;
    }

    private static bool IsPureNullPattern(PatternSyntax pattern)
    {
        PatternSyntax unwrapped = UnwrapParentheses(pattern);
        if (unwrapped is UnaryPatternSyntax { RawKind: (int)SyntaxKind.NotPattern } unary)
        {
            return IsNullOrEmptyObjectPattern(UnwrapParentheses(unary.Pattern));
        }

        return IsNullOrEmptyObjectPattern(unwrapped);
    }

    private static bool IsNullOrEmptyObjectPattern(PatternSyntax pattern)
        => pattern switch
        {
            ConstantPatternSyntax constant => IsNullLiteral(constant.Expression),
            RecursivePatternSyntax recursive => IsEmptyObjectPattern(recursive),
            _ => false,
        };

    private static bool IsEmptyObjectPattern(RecursivePatternSyntax pattern)
        => pattern.Type is null
            && pattern.PositionalPatternClause is null
            && pattern.PropertyPatternClause is { Subpatterns.Count: 0 };

    private static bool IsNullLiteral(ExpressionSyntax expression)
        => UnwrapParentheses(expression).IsKind(SyntaxKind.NullLiteralExpression);

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        ExpressionSyntax current = expression;
        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        return current;
    }

    private static PatternSyntax UnwrapParentheses(PatternSyntax pattern)
    {
        PatternSyntax current = pattern;
        while (current is ParenthesizedPatternSyntax parenthesized)
        {
            current = parenthesized.Pattern;
        }

        return current;
    }
}
