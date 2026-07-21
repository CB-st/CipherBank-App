// <copyright file="IMnemonicBackupService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Creates and opens portable encrypted mnemonic recovery files.</summary>
public interface IMnemonicBackupService
{
    Task<byte[]> CreateBackupFileAsync(
        string mnemonic,
        string recoveryPassword,
        string? hint = null,
        CancellationToken ct = default);

    Task<string> OpenBackupFileAsync(
        ReadOnlyMemory<byte> fileBytes,
        string recoveryPassword,
        CancellationToken ct = default);
}
