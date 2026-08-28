// <copyright file="CipherBankDiagnostics.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Diagnostic descriptors for repository-structure analyzers.
/// Use: High (every compilation). Scope: CipherBank-app.Analyzers.
/// </summary>
public static class CipherBankDiagnostics
{
    /// <summary>
    /// Describes CB1001.
    /// Use: High (every compilation). Scope: additional MSBuild files.
    /// </summary>
    public static readonly DiagnosticDescriptor CentralPackageVersion = new(
        "CB1001",
        "Package versions belong in Directory.Packages.props",
        "PackageReference Version attributes must live in Directory.Packages.props, not '{0}'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// Describes CB1002.
    /// Use: High (every compilation). Scope: C# syntax trees.
    /// </summary>
    public static readonly DiagnosticDescriptor LegacyAssemblyInfo = new(
        "CB1002",
        "Legacy AssemblyInfo.cs is not allowed",
        "Properties/AssemblyInfo.cs is not allowed; use SDK-style project metadata instead",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// Describes CB1003.
    /// Use: High (every Core compilation). Scope: CipherBank-app.Core C# trees.
    /// </summary>
    public static readonly DiagnosticDescriptor ScatteredSql = new(
        "CB1003",
        "Raw SQL belongs in LocalDbSql.cs",
        "Raw SQL ('{0}') is owned only by CipherBank-app.Core/Persist/Sql/LocalDbSql.cs",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Describes CB1004.
    /// Use: High (every compilation). Scope: C# identifiers.
    /// </summary>
    public static readonly DiagnosticDescriptor RetiredApiName = new(
        "CB1004",
        "Retired API name",
        "Identifier '{0}' is retired (IProductApi, MockProductApi, AppSessionDeps)",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>Gets the analyzer diagnostic category.</summary>
    public static string Category => "CipherBank.Structure";
}
