// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Path predicates shared by the structure analyzers.
/// Use: High (every compilation). Scope: CipherBank-app.Analyzers.
/// </summary>
internal static class SourcePath
{
    /// <summary>
    /// Normalizes slashes so predicates are OS-agnostic.
    /// Use: High (every path check). Scope: analyzer path helpers.
    /// </summary>
    public static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// True when the tree is a legacy Properties/AssemblyInfo.cs file.
    /// Use: High (every C# tree). Scope: NoLegacyAssemblyInfoAnalyzer.
    /// </summary>
    public static bool IsLegacyAssemblyInfo(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith("/Properties/AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Properties/AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the tree lives under CipherBank-app.Core.
    /// Use: High (every SQL check). Scope: NoScatteredSqlAnalyzer.
    /// </summary>
    public static bool IsCoreProject(string path)
    {
        string normalized = Normalize(path);
        return normalized.IndexOf("/CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.StartsWith("CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the tree is the blessed raw-SQL owner.
    /// Use: High (every SQL check). Scope: NoScatteredSqlAnalyzer.
    /// </summary>
    public static bool IsSqlOwner(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith("Persist/Sql/LocalDbSql.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the additional file is central package management.
    /// Use: High (every additional file). Scope: CentralPackageVersionAnalyzer.
    /// </summary>
    public static bool IsCentralPackageFile(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith("/Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the additional file is an MSBuild project/props/targets file.
    /// Use: High (every additional file). Scope: CentralPackageVersionAnalyzer.
    /// </summary>
    public static bool IsMsBuildProjectFile(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }
}
