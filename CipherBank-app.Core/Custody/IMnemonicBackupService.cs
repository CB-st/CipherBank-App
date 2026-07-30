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
        CancellationToken ct);

    /// <summary>Writes a backup for callers with no ambient token. Use: Low (backup export). Scope: IMnemonicBackupService consumers.</summary>
    Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword)
        => CreateBackupFileAsync(mnemonic, recoveryPassword, CancellationToken.None);

    Task<byte[]> CreateBackupFileAsync(
        string mnemonic,
        string recoveryPassword,
        string? hint,
        CancellationToken ct);

    /// <summary>Writes a hinted backup for callers with no ambient token. Use: Low (backup export). Scope: IMnemonicBackupService consumers.</summary>
    Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword, string? hint)
        => CreateBackupFileAsync(mnemonic, recoveryPassword, hint, CancellationToken.None);

    Task<string> OpenBackupFileAsync(
        ReadOnlyMemory<byte> fileBytes,
        string recoveryPassword,
        CancellationToken ct);

    /// <summary>Reads a backup for callers with no ambient token. Use: Low (restore). Scope: IMnemonicBackupService consumers.</summary>
    Task<string> OpenBackupFileAsync(ReadOnlyMemory<byte> fileBytes, string recoveryPassword)
        => OpenBackupFileAsync(fileBytes, recoveryPassword, CancellationToken.None);
}
