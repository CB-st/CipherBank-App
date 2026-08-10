// <copyright file="IBackupFileService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Platform file save/share (export) and pick (restore) for the mnemonic recovery file.
/// Never touches the network — writes/reads local storage only.
/// </summary>
public interface IBackupFileService
{
    /// <summary>
    /// Writes the recovery file to a durable, user-reachable device location that outlives the app's own
    /// storage, and returns a display location for it (null when the platform offers none).
    /// Use: Low (once per export). Scope: device shared storage.
    /// </summary>
    Task<string?> SaveRecoveryFileAsync(byte[] fileBytes, string suggestedFileName, CancellationToken ct = default);

    /// <summary>
    /// Stages a copy in app cache and hands it to the OS share sheet, for users who also want the file
    /// off the device. Use: Low (only when the user opts into sharing). Scope: app cache + OS share sheet.
    /// </summary>
    Task ShareRecoveryFileAsync(byte[] fileBytes, string suggestedFileName, CancellationToken ct = default);

    /// <summary>Prompts the user to pick a file and returns its bytes, or null if cancelled.</summary>
    Task<byte[]?> PickBackupFileAsync(CancellationToken ct = default);
}
