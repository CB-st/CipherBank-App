// <copyright file="NullCheckSyntax.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Extracts checked operands from pure null-check syntax constructs.
/// Use: High (every compilation). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
/// </summary>
internal static class NullCheckSyntax
{
    /// <summary>
    /// Extracts the invoked method identifier when syntactically present.
    /// Use: High (every invocation). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static string? GetInvokedMethodName(ExpressionSyntax expression)
        => expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
            _ => null,
        };

    /// <summary>
    /// Returns the checked operand for <c>ArgumentNullException.ThrowIfNull</c> or
    /// <c>object.ReferenceEquals</c>/<c>object.Equals</c> against a <c>null</c> literal.
    /// Use: High (matching invocations). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static ExpressionSyntax? GetInvocationCheckedOperand(
        InvocationExpressionSyntax invocation,
        ISymbol method,
        ISymbol? argumentNullExceptionType)
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

    /// <summary>
    /// Returns the non-null-literal side of a binary equality expression when exactly one side is <c>null</c>.
    /// Use: High (every <c>==</c>/<c>!=</c>). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static ExpressionSyntax? GetNullComparisonOperand(BinaryExpressionSyntax binary)
        => GetNullPairOperand(binary.Left, binary.Right);

    /// <summary>
    /// True when <paramref name="pattern"/> is <c>null</c>, <c>not null</c>, <c>{ }</c>, or <c>not { }</c>.
    /// Use: High (every <c>is</c> pattern). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static bool IsPureNullPattern(PatternSyntax pattern)
    {
        PatternSyntax unwrapped = UnwrapParentheses(pattern);
        if (unwrapped is UnaryPatternSyntax { RawKind: (int)SyntaxKind.NotPattern } unary)
        {
            return IsNullOrEmptyObjectPattern(UnwrapParentheses(unary.Pattern));
        }

        return IsNullOrEmptyObjectPattern(unwrapped);
    }

    /// <summary>
    /// Peels enclosing parenthesized expressions.
    /// Use: High (every candidate operand). Scope: <see cref="NoNonNullableNullCheckAnalyzer"/>.
    /// </summary>
    internal static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        ExpressionSyntax current = expression;
        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        return current;
    }

    private static ExpressionSyntax? FindArgumentExpression(
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        string parameterName)
    {
        ArgumentSyntax? named = arguments.FirstOrDefault(
            argument => argument.NameColon?.Name.Identifier.ValueText == parameterName);
        if (named is not null)
        {
            return named.Expression;
        }

        return arguments.Count > 0 && arguments[0].NameColon is null
            ? arguments[0].Expression
            : null;
    }

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
