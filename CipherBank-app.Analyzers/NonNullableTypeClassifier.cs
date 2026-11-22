// <copyright file="NonNullableTypeClassifier.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Determines whether a C# expression is statically known to have a non-nullable type.
/// Use: High (every candidate null check). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
/// </summary>
internal static class NonNullableTypeClassifier
{
    /// <summary>
    /// Returns the minimally qualified type display name when <paramref name="expression"/>
    /// has a non-nullable value, reference, or constrained generic type; otherwise <c>null</c>.
    /// Use: High (every candidate null check). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static string? TryGetNonNullableTypeName(
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
        => method.GetReturnTypeAttributes().Any(IsMaybeNullAttribute);

    private static bool IsMaybeNullAttribute(AttributeData attribute)
        => attribute.AttributeClass is
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
        };

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
}
