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
    /// <summary>PackageReference Version attribute outside Directory.Packages.props.</summary>
    public const string CentralPackageVersionId = "CB1001";

    /// <summary>Legacy Properties/AssemblyInfo.cs is forbidden.</summary>
    public const string LegacyAssemblyInfoId = "CB1002";

    /// <summary>Raw SQL outside LocalDbSql.cs in Core.</summary>
    public const string ScatteredSqlId = "CB1003";

    /// <summary>Retired API identifier.</summary>
    public const string RetiredApiNameId = "CB1004";

    /// <summary>
    /// Describes CB1001.
    /// Use: High (every compilation). Scope: additional MSBuild files.
    /// </summary>
    public static readonly DiagnosticDescriptor CentralPackageVersion = new(
        CentralPackageVersionId,
        "Package versions belong in Directory.Packages.props",
        "PackageReference Version attributes must live in Directory.Packages.props, not '{0}'",
        "CipherBank.Structure",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Describes CB1002.
    /// Use: High (every compilation). Scope: C# syntax trees.
    /// </summary>
    public static readonly DiagnosticDescriptor LegacyAssemblyInfo = new(
        LegacyAssemblyInfoId,
        "Legacy AssemblyInfo.cs is not allowed",
        "Properties/AssemblyInfo.cs is not allowed; use SDK-style project metadata instead",
        "CipherBank.Structure",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Describes CB1003.
    /// Use: High (every Core compilation). Scope: CipherBank-app.Core C# trees.
    /// </summary>
    public static readonly DiagnosticDescriptor ScatteredSql = new(
        ScatteredSqlId,
        "Raw SQL belongs in LocalDbSql.cs",
        "Raw SQL ('{0}') is owned only by CipherBank-app.Core/Persist/Sql/LocalDbSql.cs",
        "CipherBank.Structure",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Describes CB1004.
    /// Use: High (every compilation). Scope: C# identifiers.
    /// </summary>
    public static readonly DiagnosticDescriptor RetiredApiName = new(
        RetiredApiNameId,
        "Retired API name",
        "Identifier '{0}' is retired (IProductApi, MockProductApi, AppSessionDeps)",
        "CipherBank.Structure",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
