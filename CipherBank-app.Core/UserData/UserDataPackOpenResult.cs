// <copyright file="UserDataPackOpenResult.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Outcome of opening a userdata pack: authenticated blocks plus the blocks that failed.
/// Use: High (GRAB apply). Scope: userdata restore.
/// </summary>
/// <remarks>
/// A restore is only complete when <see cref="FailedBlocks"/> is empty. Callers must mark each
/// failed block type dirty (or abort) instead of treating a partial open as a full restore.
/// </remarks>
public sealed class UserDataPackOpenResult
{
    internal UserDataPackOpenResult(
        IReadOnlyDictionary<string, string> blocks,
        IReadOnlyList<UserDataBlockFailure> failedBlocks)
    {
        Blocks = blocks;
        FailedBlocks = failedBlocks;
    }

    /// <summary>Gets the authenticated plaintext blocks, keyed by block id.</summary>
    public IReadOnlyDictionary<string, string> Blocks { get; }

    /// <summary>Gets malformed blocks and blocks that failed authentication, in pack order.</summary>
    /// <remarks>A missing, null, empty, or whitespace-only block id or type is represented by <see cref="string.Empty"/>.</remarks>
    public IReadOnlyList<UserDataBlockFailure> FailedBlocks { get; }

    /// <summary>Gets a value indicating whether the pack was opened without block failures.</summary>
    public bool IsComplete => FailedBlocks.Count == 0;
}
