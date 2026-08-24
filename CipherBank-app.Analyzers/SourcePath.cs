// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Host-native path wrapper for analyzer additional files.
/// Uses <see cref="Path.GetFileName"/> / <see cref="Path.GetExtension"/> /
/// <see cref="Path.GetDirectoryName"/>. Do not call <see cref="Path.GetFullPath"/>
/// or Combine on additional-file strings.
/// </summary>
internal readonly struct SourcePath : IEquatable<SourcePath>
{
    private readonly string _path;

    private SourcePath(string path) => _path = path;

    /// <summary>Gets the wrapped path string.</summary>
    internal string Value => _path;

    /// <summary>Gets the file name via <see cref="Path.GetFileName"/>.</summary>
    internal string FileName => Path.GetFileName(_path);

    /// <summary>Gets the extension via <see cref="Path.GetExtension"/>.</summary>
    internal string Extension => Path.GetExtension(_path);

    /// <summary>Gets the directory via <see cref="Path.GetDirectoryName"/>.</summary>
    internal string DirectoryName => Path.GetDirectoryName(_path) ?? string.Empty;

    /// <summary>Gets a value indicating whether the additional file is C# source.</summary>
    internal bool IsCSharpFile
        => string.Equals(Extension, ".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the tree is Properties/AssemblyInfo.cs.</summary>
    internal bool IsLegacyAssemblyInfo
        => string.Equals(FileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFileName(DirectoryName), "Properties", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the tree lives under CipherBank-app.Core.</summary>
    internal bool IsCoreProject
    {
        get
        {
            for (string? current = _path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                if (string.Equals(Path.GetFileName(current), "CipherBank-app.Core", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the tree is Persist/Sql/LocalDbSql.cs (CB1003 owner).
    /// </summary>
    internal bool IsSqlOwner
        => string.Equals(FileName, "LocalDbSql.cs", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFileName(DirectoryName), "Sql", StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                Path.GetFileName(Path.GetDirectoryName(DirectoryName)),
                "Persist",
                StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the file is Directory.Packages.props.</summary>
    internal bool IsCentralPackageFile
        => string.Equals(FileName, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets a value indicating whether the file is .csproj, .props, or .targets.</summary>
    internal bool IsMsBuildProjectFile
        => string.Equals(Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Extension, ".props", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Extension, ".targets", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public bool Equals(SourcePath other)
        => string.Equals(_path, other._path, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SourcePath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_path);

    /// <inheritdoc/>
    public override string ToString() => _path;

    /// <summary>Wraps an MSBuild/Roslyn host path string.</summary>
    internal static SourcePath From(string? path) => new(path ?? string.Empty);
}
