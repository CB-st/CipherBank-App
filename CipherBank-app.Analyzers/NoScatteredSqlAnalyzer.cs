// <copyright file="NoScatteredSqlAnalyzer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Flags raw SQL in CipherBank-app.Core.
/// Use: High (every Core compilation). Scope: Core C# trees.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoScatteredSqlAnalyzer : DiagnosticAnalyzer
{
    private const string CommandTextName = "CommandText";

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
    /// Registers assignment and invocation actions for Core trees.
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
    }

    /// <summary>
    /// Reports CommandText assignments in Core.
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
    /// Reports FromSqlRaw / ExecuteSqlRaw invocations in Core.
    /// Use: High (every invocation). Scope: Core C# trees.
    /// </summary>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!ShouldScan(context.Node.SyntaxTree.FilePath))
        {
            return;
        }

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        string? name = MethodName(invocation);
        if (name is null || !_rawSqlMethods.Contains(name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ScatteredSql,
            invocation.GetLocation(),
            name));
    }

    /// <summary>
    /// True when the tree is Core.
    /// Use: High (every SQL syntax action). Scope: this analyzer.
    /// </summary>
    private static bool ShouldScan(string path)
        => SourcePath.From(path).IsCoreProject;

    /// <summary>
    /// Returns the invoked method identifier, if any.
    /// Use: High (every invocation). Scope: this analyzer.
    /// </summary>
    private static string? MethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
            SimpleNameSyntax name => name.Identifier.ValueText,
            _ => null,
        };
    }

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
            IdentifierNameSyntax identifier when identifier.Identifier.ValueText == CommandTextName
                => identifier.Identifier,
            _ => null,
        };
    }
}
