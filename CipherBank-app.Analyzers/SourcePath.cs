// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Predicates over the original Roslyn/MSBuild additional-file path string.
/// Works on the string as supplied (no <see cref="Path.GetFullPath"/>, no filesystem access)
/// so analyzer decisions stay deterministic across hosts.
/// </summary>
internal sealed class SourcePath
{
    private readonly string _path;

    private SourcePath(string path)
    {
        _path = path;
    }

    /// <summary>Gets the last segment via <see cref="Path.GetFileName"/>.</summary>
    internal string FileName => Path.GetFileName(_path);

    /// <summary>Gets the extension via <see cref="Path.GetExtension"/>.</summary>
    internal string Extension => Path.GetExtension(_path);

    /// <summary>Gets the parent path via <see cref="Path.GetDirectoryName"/>.</summary>
    internal SourcePath Parent => From(Path.GetDirectoryName(_path));

    /// <summary>Gets a value indicating whether this path has no segments left.</summary>
    internal bool IsEmpty => string.IsNullOrEmpty(_path);

    /// <summary>Gets a value indicating whether the additional file is C# source.</summary>
    internal bool IsCSharpFile
        => string.Equals(Extension, ".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the tree is Properties/AssemblyInfo.cs.</summary>
    internal bool IsLegacyAssemblyInfo
        => string.Equals(FileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Parent.FileName, "Properties", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether a directory segment is CipherBank-app.Core.</summary>
    internal bool IsCoreProject
    {
        get
        {
            for (SourcePath current = this; !current.IsEmpty; current = current.Parent)
            {
                if (string.Equals(current.FileName, "CipherBank-app.Core", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Gets a value indicating whether the additional file is Directory.Packages.props.</summary>
    internal bool IsCentralPackageFile
        => string.Equals(FileName, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the additional file is .csproj, .props, or .targets.</summary>
    internal bool IsMsBuildProjectFile
        => string.Equals(Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Extension, ".props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Extension, ".targets", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Compares original additional-file strings, ignoring case. <see cref="FileInfo"/> equality is
    /// reference-based and resolving <c>FullName</c> would call GetFullPath.
    /// Use: High (compilation tree dedupe). Scope: analyzer additional files.
    /// </summary>
    internal static bool NamesEqual(SourcePath left, SourcePath right)
        => string.Equals(left._path, right._path, StringComparison.OrdinalIgnoreCase);

    /// <summary>Wraps a Roslyn/MSBuild additional-file path.</summary>
    internal static SourcePath From(string? path) => new(path ?? string.Empty);
}
