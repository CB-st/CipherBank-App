// <copyright file="RepoPaths.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Resolves the git repo root from the current working directory so artifact writers (journal, gap notes)
/// land in stable, repo-relative paths regardless of the test host's actual working directory (dotnet test
/// runs with cwd = the test assembly's bin/Debug output dir, not the repo root).
/// Use: Medium (every artifact write: journal flush, gap note write). Scope: process-wide path resolution.
/// </summary>
public static class RepoPaths
{
    /// <summary>
    /// Walks upward from cwd to the nearest ancestor containing a .git directory or worktree .git file;
    /// falls back to cwd if none is found (e.g. running outside a git checkout).
    /// Use: Medium. Scope: artifact path resolution.
    /// </summary>
    public static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            string gitMarker = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitMarker) || File.Exists(gitMarker))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Resolves a possibly-relative artifact path against the repo root (absolute paths pass through
    /// unchanged), so env-var overrides like E2E_JOURNAL_DIR still work from any cwd.
    /// Use: Medium. Scope: artifact path resolution.
    /// </summary>
    public static string ResolveFromRoot(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(FindRoot(), path);
}
