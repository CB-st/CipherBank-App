// <copyright file="AdditionalSource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CipherBank_app.Analyzers;

/// <summary>
/// Parses additional C# files that are not part of the current compilation.
/// Use: High (every compilation). Scope: CipherBank-app.Analyzers.
/// </summary>
internal static class AdditionalSource
{
    /// <summary>
    /// True when <paramref name="path"/> is not already a compilation syntax tree.
    /// Use: High (every additional file). Scope: sibling-project scans.
    /// </summary>
    internal static bool IsOutsideCompilation(Compilation compilation, string path)
    {
        string normalized = SourcePath.Normalize(path);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (SourcePath.PathsEqual(tree.FilePath, normalized))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses an additional C# file for token walks. Locations must use
    /// <see cref="CreateLocation"/> so Roslyn accepts the additional-file path.
    /// Use: High (every additional C# file). Scope: sibling-project scans.
    /// </summary>
    internal static bool TryParseCSharp(
        AdditionalText file,
        CancellationToken cancellationToken,
        out SyntaxTree tree,
        out SourceText text)
    {
        tree = null!;
        text = null!;
        if (!SourcePath.IsCSharpFile(file.Path))
        {
            return false;
        }

        SourceText? loaded = file.GetText(cancellationToken);
        if (loaded is null)
        {
            return false;
        }

        text = loaded;
        tree = CSharpSyntaxTree.ParseText(text, path: file.Path, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>
    /// Builds a location on an additional file that is part of the compilation.
    /// Use: High (each additional-file diagnostic). Scope: sibling-project scans.
    /// </summary>
    internal static Location CreateLocation(string path, SourceText text, TextSpan span)
        => Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
}
