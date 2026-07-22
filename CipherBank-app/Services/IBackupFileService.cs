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
    /// <summary>Writes bytes to app storage and hands off to the OS share/save sheet.</summary>
    Task<bool> SaveAndShareAsync(byte[] fileBytes, string suggestedFileName, CancellationToken ct = default);

    /// <summary>Prompts the user to pick a file and returns its bytes, or null if cancelled.</summary>
    Task<byte[]?> PickBackupFileAsync(CancellationToken ct = default);
}
