// <copyright file="MnemonicBackupService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherBank_app.Custody;

/// <summary>PBKDF2 and AES-GCM protection for portable mnemonic recovery files.</summary>
public sealed class MnemonicBackupService : IMnemonicBackupService
{
    private const string Format = "cipherbank-recovery-v1";
    private const string InvalidRecoveryFileMessage = "Invalid recovery file.";
    private const string Kdf = "PBKDF2-SHA256";
    private const int Iterations = 600_000;
    private const int MinimumPasswordLength = 12;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    public Task<byte[]> CreateBackupFileAsync(
        string mnemonic,
        string recoveryPassword,
        CancellationToken ct)
        => CreateBackupFileAsync(mnemonic, recoveryPassword, null, ct);

    public Task<byte[]> CreateBackupFileAsync(
        string mnemonic,
        string recoveryPassword,
        string? hint,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePassword(recoveryPassword);
        if (!MnemonicHelper.Validate(mnemonic))
        {
            throw new ArgumentException("Mnemonic is invalid.", nameof(mnemonic));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] key = DeriveKey(recoveryPassword, salt);
        byte[] plaintext = Encoding.UTF8.GetBytes(MnemonicHelper.Normalize(mnemonic));
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var document = new BackupDocument
            {
                DocumentFormat = Format,
                KeyDerivation = Kdf,
                IterationCount = Iterations,
                SaltBase64 = Convert.ToBase64String(salt),
                NonceBase64 = Convert.ToBase64String(nonce),
                TagBase64 = Convert.ToBase64String(tag),
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
                CreatedAt = DateTimeOffset.UtcNow,
                Hint = hint,
            };

            return Task.FromResult(JsonSerializer.SerializeToUtf8Bytes(document));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public Task<string> OpenBackupFileAsync(
        ReadOnlyMemory<byte> fileBytes,
        string recoveryPassword,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ValidatePassword(recoveryPassword);

        BackupDocument document;
        try
        {
            document = JsonSerializer.Deserialize<BackupDocument>(fileBytes.Span)
                ?? throw new CryptographicException(InvalidRecoveryFileMessage);
        }
        catch (JsonException ex)
        {
            throw new CryptographicException(InvalidRecoveryFileMessage, ex);
        }

        ValidateDocument(document);

        byte[] salt;
        byte[] nonce;
        byte[] tag;
        byte[] ciphertext;
        try
        {
            salt = Convert.FromBase64String(document.SaltBase64);
            nonce = Convert.FromBase64String(document.NonceBase64);
            tag = Convert.FromBase64String(document.TagBase64);
            ciphertext = Convert.FromBase64String(document.CiphertextBase64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException(InvalidRecoveryFileMessage, ex);
        }

        if (salt.Length != SaltSize ||
            nonce.Length != NonceSize ||
            tag.Length != TagSize ||
            ciphertext.Length == 0)
        {
            throw new CryptographicException(InvalidRecoveryFileMessage);
        }

        byte[] key = DeriveKey(recoveryPassword, salt);
        byte[] plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            string mnemonic = Encoding.UTF8.GetString(plaintext);
            if (!MnemonicHelper.Validate(mnemonic))
            {
                throw new CryptographicException("Recovery file does not contain a valid mnemonic.");
            }

            return Task.FromResult(MnemonicHelper.Normalize(mnemonic));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static byte[] DeriveKey(string recoveryPassword, byte[] salt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(recoveryPassword);
        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static void ValidatePassword(string recoveryPassword)
    {
        ArgumentNullException.ThrowIfNull(recoveryPassword);
        if (recoveryPassword.Length < MinimumPasswordLength)
        {
            throw new ArgumentException(
                $"Recovery password must be at least {MinimumPasswordLength} characters.",
                nameof(recoveryPassword));
        }
    }

    /// <summary>
    /// Rejects recovery files whose header, ciphertext fields, or timestamp are missing/mismatched.
    /// Use: High (every recovery-file open). Scope: MnemonicBackupService.OpenBackupFileAsync.
    /// </summary>
    private static void ValidateDocument(BackupDocument document)
    {
        if (HasUnsupportedHeader(document) || HasMissingCryptoFields(document) || document.CreatedAt == default)
        {
            throw new CryptographicException("Unsupported or invalid recovery file.");
        }
    }

    /// <summary>
    /// Checks the recovery file header against the expected format/KDF/iteration-count constants.
    /// Use: High (every recovery-file open, via ValidateDocument). Scope: MnemonicBackupService.ValidateDocument.
    /// </summary>
    private static bool HasUnsupportedHeader(BackupDocument document)
        => document.DocumentFormat != Format
            || document.KeyDerivation != Kdf
            || document.IterationCount != Iterations;

    /// <summary>
    /// Checks whether any required ciphertext field (salt/nonce/tag/ciphertext) is blank.
    /// Use: High (every recovery-file open, via ValidateDocument). Scope: MnemonicBackupService.ValidateDocument.
    /// </summary>
    private static bool HasMissingCryptoFields(BackupDocument document)
        => string.IsNullOrWhiteSpace(document.SaltBase64)
            || string.IsNullOrWhiteSpace(document.NonceBase64)
            || string.IsNullOrWhiteSpace(document.TagBase64)
            || string.IsNullOrWhiteSpace(document.CiphertextBase64);

    private sealed class BackupDocument
    {
        [JsonPropertyName("FORMAT")]
        public string DocumentFormat { get; init; } = string.Empty;

        [JsonPropertyName("KDF")]
        public string KeyDerivation { get; init; } = string.Empty;

        [JsonPropertyName("ITERATIONS")]
        public int IterationCount { get; init; }

        [JsonPropertyName("SALT_B64")]
        public string SaltBase64 { get; init; } = string.Empty;

        [JsonPropertyName("NONCE_B64")]
        public string NonceBase64 { get; init; } = string.Empty;

        [JsonPropertyName("TAG_B64")]
        public string TagBase64 { get; init; } = string.Empty;

        [JsonPropertyName("CIPHERTEXT_B64")]
        public string CiphertextBase64 { get; init; } = string.Empty;

        [JsonPropertyName("CREATED_AT")]
        public DateTimeOffset CreatedAt { get; init; }

        [JsonPropertyName("HINT")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Hint { get; init; }
    }
}
