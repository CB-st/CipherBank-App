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
    internal static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// True when two filesystem paths match after slash normalization.
    /// Use: High (every additional-file dedupe). Scope: analyzer path helpers.
    /// </summary>
    internal static bool PathsEqual(string left, string right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when the additional file is C# source.
    /// Use: High (every additional file). Scope: sibling-project scans.
    /// </summary>
    internal static bool IsCSharpFile(string path)
        => Normalize(path).EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when the tree is a legacy Properties/AssemblyInfo.cs file.
    /// Use: High (every C# tree). Scope: NoLegacyAssemblyInfoAnalyzer.
    /// </summary>
    internal static bool IsLegacyAssemblyInfo(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith("/Properties/AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Properties/AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the tree lives under CipherBank-app.Core.
    /// Use: High (every SQL check). Scope: NoScatteredSqlAnalyzer.
    /// </summary>
    internal static bool IsCoreProject(string path)
    {
        string normalized = Normalize(path);
        return normalized.IndexOf("/CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.StartsWith("CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the additional file is central package management.
    /// Use: High (every additional file). Scope: CentralPackageVersionAnalyzer.
    /// </summary>
    internal static bool IsCentralPackageFile(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith("/Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when the additional file is an MSBuild project/props/targets file.
    /// Use: High (every additional file). Scope: CentralPackageVersionAnalyzer.
    /// </summary>
    internal static bool IsMsBuildProjectFile(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }
}
