// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Path extensions for analyzer additional-file strings.
/// Uses <see cref="Path.GetFileName"/> / <see cref="Path.GetExtension"/> /
/// <see cref="Path.GetDirectoryName"/> on host-native paths. Do not call
/// <see cref="Path.GetFullPath"/> or Combine on additional-file strings, and do
/// not rewrite path separators.
/// </summary>
internal static class SourcePath
{
    /// <summary>True when two host paths match (case-insensitive).</summary>
    internal static bool PathsEqual(this string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the additional file is C# source.</summary>
    internal static bool IsCSharpFile(this string path)
        => string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the tree is Properties/AssemblyInfo.cs.</summary>
    internal static bool IsLegacyAssemblyInfo(this string path)
    {
        if (!string.Equals(Path.GetFileName(path), "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        return string.Equals(Path.GetFileName(directory), "Properties", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when the tree lives under CipherBank-app.Core.</summary>
    internal static bool IsCoreProject(this string path)
    {
        for (string? current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            if (string.Equals(Path.GetFileName(current), "CipherBank-app.Core", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the additional file is Directory.Packages.props.</summary>
    internal static bool IsCentralPackageFile(this string path)
        => string.Equals(Path.GetFileName(path), "Directory.Packages.props", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the additional file is .csproj, .props, or .targets.</summary>
    internal static bool IsMsBuildProjectFile(this string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".targets", StringComparison.OrdinalIgnoreCase);
    }
}
