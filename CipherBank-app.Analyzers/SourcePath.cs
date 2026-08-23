// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Path extensions for analyzer additional-file strings.
/// Slash-normalize, then <see cref="System.IO.Path.GetFileName"/> / <see cref="System.IO.Path.GetExtension"/>.
/// Do not use <see cref="System.IO.Path.GetFullPath"/> or Combine on additional files.
/// </summary>
internal static class SourcePath
{
    /// <summary>Slash-normalize so Path filename/extension APIs are OS-agnostic.</summary>
    internal static string NormalizeSlashes(this string path) => path.Replace('\\', '/');

    /// <summary>True when two paths match after slash normalization.</summary>
    internal static bool PathsEqual(this string left, string right)
        => string.Equals(left.NormalizeSlashes(), right.NormalizeSlashes(), StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the additional file is C# source.</summary>
    internal static bool IsCSharpFile(this string path)
        => string.Equals(Path.GetExtension(path.NormalizeSlashes()), ".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the tree is Properties/AssemblyInfo.cs.</summary>
    internal static bool IsLegacyAssemblyInfo(this string path)
    {
        string normalized = path.NormalizeSlashes();
        if (!string.Equals(Path.GetFileName(normalized), "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string directory = Path.GetDirectoryName(normalized) ?? string.Empty;
        return string.Equals(Path.GetFileName(directory), "Properties", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when the tree lives under CipherBank-app.Core.</summary>
    internal static bool IsCoreProject(this string path)
    {
        string normalized = path.NormalizeSlashes();
        return normalized.IndexOf("/CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.StartsWith("CipherBank-app.Core/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when the additional file is Directory.Packages.props.</summary>
    internal static bool IsCentralPackageFile(this string path)
        => string.Equals(
            Path.GetFileName(path.NormalizeSlashes()),
            "Directory.Packages.props",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the additional file is .csproj, .props, or .targets.</summary>
    internal static bool IsMsBuildProjectFile(this string path)
    {
        string extension = Path.GetExtension(path.NormalizeSlashes());
        return string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".targets", StringComparison.OrdinalIgnoreCase);
    }
}
