// <copyright file="BackupFileService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Provides Tizen shared-storage support for recovery exports.</summary>
public sealed partial class BackupFileService
{
    /// <summary>
    /// Non-Android hosts have no shared-storage collection wired up yet, so the export finishes through the
    /// share sheet alone. Use: Low (once per export). Scope: this platform build.
    /// </summary>
    private static partial Task<string?> SaveToSharedStorageAsync(byte[] fileBytes, string fileName, CancellationToken ct)
        => Task.FromResult<string?>(null);
}
