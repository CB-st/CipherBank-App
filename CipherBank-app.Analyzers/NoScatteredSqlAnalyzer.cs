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

    private static readonly HashSet<string> RawSqlMethods = new(StringComparer.Ordinal)
    {
        "FromSqlRaw",
        "ExecuteSqlRaw",
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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
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
        if (assignment.Left is not MemberAccessExpressionSyntax member
            || member.Name.Identifier.ValueText != CommandTextName)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CipherBankDiagnostics.ScatteredSql,
            member.Name.GetLocation(),
            member.Name.Identifier.ValueText));
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
        if (name is null || !RawSqlMethods.Contains(name))
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
        => path.IsCoreProject();

    /// <summary>
    /// Returns the invoked method identifier, if any.
    /// Use: High (every invocation). Scope: this analyzer.
    /// </summary>
    private static string? MethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };
    }
}
