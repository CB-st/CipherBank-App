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

    /// <summary>Gets the blocks that failed authentication or decoding, in pack order.</summary>
    public IReadOnlyList<UserDataBlockFailure> FailedBlocks { get; }

    /// <summary>Gets a value indicating whether every opened block authenticated.</summary>
    public bool IsComplete => FailedBlocks.Count == 0;
}
