// <copyright file="SourcePath.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Analyzers;

/// <summary>
/// Host-native additional-file path. Segments come from <see cref="Path.GetFileName"/>,
/// <see cref="Path.GetExtension"/>, and <see cref="Path.GetDirectoryName"/>.
/// Do not call <see cref="Path.GetFullPath"/> or Combine on additional-file strings,
/// and do not rewrite path separators.
/// </summary>
internal readonly struct SourcePath : IEquatable<SourcePath>
{
    private readonly string _value;

    private SourcePath(string value) => _value = value;

    /// <summary>Gets the host-native path string Roslyn supplied.</summary>
    internal string Value => _value;

    /// <summary>Gets the last segment via <see cref="Path.GetFileName"/>.</summary>
    internal string FileName => Path.GetFileName(_value);

    /// <summary>Gets the extension via <see cref="Path.GetExtension"/>.</summary>
    internal string Extension => Path.GetExtension(_value);

    /// <summary>Gets the parent path via <see cref="Path.GetDirectoryName"/>.</summary>
    internal SourcePath Parent => From(Path.GetDirectoryName(_value));

    /// <summary>Gets a value indicating whether this path has no segments left.</summary>
    internal bool IsEmpty => string.IsNullOrEmpty(_value);

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

    /// <inheritdoc />
    public bool Equals(SourcePath other)
        => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SourcePath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_value);

    /// <inheritdoc />
    public override string ToString() => _value;

    /// <summary>Wraps a Roslyn/MSBuild additional-file path.</summary>
    internal static SourcePath From(string? path) => new(path ?? string.Empty);
}
