// <copyright file="UserDataPackCodec.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CipherBank_app.UserData;

/// <summary>
/// Seals/opens cipherbank-userdata-pack-v1 blocks and Base64-encodes the pack as USER_DATA_BLOB.
/// </summary>
public static class UserDataPackCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    /// <summary>
    /// Seals plaintext blocks under the KEK and builds a pack envelope for the username.
    /// Use: High (prefs push). Scope: userdata sync.
    /// </summary>
    public static UserDataPackWire SealPack(
        string username,
        uint contentVersion,
        ReadOnlySpan<byte> kek,
        IReadOnlyList<UserDataPlainBlock> plainBlocks)
    {
        ArgumentNullException.ThrowIfNull(plainBlocks);
        EnsureKek(kek);

        string usernameHashHex = UserDataUsernameHash.HashHex(username);
        UserDataPackWire pack = new()
        {
            Format = UserDataConstants.PackFormat,
            ContentVersion = contentVersion,
            UsernameHashPrefix = UserDataUsernameHash.HashPrefix(username),
        };

        foreach (UserDataPlainBlock plain in plainBlocks)
        {
            pack.Blocks.Add(SealBlock(plain, kek, usernameHashHex, contentVersion));
        }

        return pack;
    }

    /// <summary>
    /// Seals a single UTF-8 payload into a wire block.
    /// Use: High (SealPack). Scope: UserDataPackCodec.
    /// </summary>
    public static UserDataBlockWire SealBlock(
        UserDataPlainBlock plain,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion)
    {
        ArgumentNullException.ThrowIfNull(plain);
        EnsureKek(kek);

        byte[] nonce = RandomNumberGenerator.GetBytes(UserDataConstants.NonceSize);
        byte[] plaintext = Encoding.UTF8.GetBytes(plain.PlaintextUtf8);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[UserDataConstants.TagSize];
        byte[] aad = UserDataAad.Build(usernameHashHex, plain.Type, plain.Id, contentVersion);

        try
        {
            using var aes = new AesGcm(kek, UserDataConstants.TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

            return new UserDataBlockWire
            {
                Id = plain.Id,
                Type = plain.Type,
                Seq = plain.Seq,
                Algorithm = UserDataConstants.BlockAlgorithm,
                NonceBase64 = Convert.ToBase64String(nonce),
                TagBase64 = Convert.ToBase64String(tag),
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    /// <summary>
    /// Opens one block; GCM failure throws <see cref="CryptographicException"/>.
    /// Use: High (restore). Scope: UserDataPackCodec.
    /// </summary>
    public static string OpenBlock(
        UserDataBlockWire block,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion)
    {
        ArgumentNullException.ThrowIfNull(block);
        EnsureKek(kek);
        ValidateBlockHeader(block);

        byte[] nonce = DecodeExact(block.NonceBase64, UserDataConstants.NonceSize, "nonce");
        byte[] tag = DecodeExact(block.TagBase64, UserDataConstants.TagSize, "tag");
        byte[] ciphertext = DecodeAtLeast(block.CiphertextBase64, minLength: 1, "ciphertext");
        byte[] plaintext = new byte[ciphertext.Length];
        byte[] aad = UserDataAad.Build(usernameHashHex, block.Type, block.Id, contentVersion);

        try
        {
            using var aes = new AesGcm(kek, UserDataConstants.TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(aad);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    /// <summary>
    /// Opens every block; skips unknown types that fail authentication only when skipUnknown is true.
    /// Use: High (GRAB apply). Scope: userdata restore.
    /// </summary>
    public static Dictionary<string, string> OpenPack(
        UserDataPackWire pack,
        string username,
        ReadOnlySpan<byte> kek,
        bool skipUnknownTypes = true)
    {
        ArgumentNullException.ThrowIfNull(pack);
        EnsureKek(kek);
        ValidatePackHeader(pack, username);

        string usernameHashHex = UserDataUsernameHash.HashHex(username);
        Dictionary<string, string> opened = new(StringComparer.Ordinal);

        foreach (UserDataBlockWire block in pack.Blocks)
        {
            if (skipUnknownTypes && !UserDataBlockTypes.IsKnown(block.Type))
            {
                continue;
            }

            opened[block.Id] = OpenBlock(block, kek, usernameHashHex, pack.ContentVersion);
        }

        return opened;
    }

    /// <summary>
    /// Serializes the pack envelope to Base64 UTF-8 JSON (USER_DATA_BLOB wire shape).
    /// Use: High (OVERWRITE). Scope: userdata client.
    /// </summary>
    public static string EncodeBlob(UserDataPackWire pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(pack, JsonOptions);
        return Convert.ToBase64String(utf8);
    }

    /// <summary>
    /// Parses a Base64 USER_DATA_BLOB into a pack envelope (no decrypt).
    /// Use: High (GRAB). Scope: userdata client.
    /// </summary>
    public static UserDataPackWire DecodeBlob(string userDataBlobBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataBlobBase64);

        byte[] utf8;
        try
        {
            utf8 = Convert.FromBase64String(userDataBlobBase64);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Invalid userdata blob encoding.", ex);
        }

        UserDataPackWire? pack;
        try
        {
            pack = JsonSerializer.Deserialize<UserDataPackWire>(utf8, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new CryptographicException("Invalid userdata pack JSON.", ex);
        }

        if (pack is null)
        {
            throw new CryptographicException("Invalid userdata pack JSON.");
        }

        if (pack.Format != UserDataConstants.PackFormat)
        {
            throw new CryptographicException("Unsupported userdata pack format.");
        }

        return pack;
    }

    /// <summary>
    /// Seals a single block using an injected nonce (test vectors only).
    /// Use: Low (unit tests). Scope: UserDataPackCodecTests.
    /// </summary>
    internal static UserDataBlockWire SealBlockWithNonce(
        UserDataPlainBlock plain,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion,
        ReadOnlySpan<byte> nonce)
    {
        ArgumentNullException.ThrowIfNull(plain);
        EnsureKek(kek);
        if (nonce.Length != UserDataConstants.NonceSize)
        {
            throw new ArgumentException($"Nonce must be {UserDataConstants.NonceSize} bytes.", nameof(nonce));
        }

        byte[] plaintext = Encoding.UTF8.GetBytes(plain.PlaintextUtf8);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[UserDataConstants.TagSize];
        byte[] aad = UserDataAad.Build(usernameHashHex, plain.Type, plain.Id, contentVersion);
        byte[] nonceBytes = nonce.ToArray();

        try
        {
            using var aes = new AesGcm(kek, UserDataConstants.TagSize);
            aes.Encrypt(nonceBytes, plaintext, ciphertext, tag, aad);

            return new UserDataBlockWire
            {
                Id = plain.Id,
                Type = plain.Type,
                Seq = plain.Seq,
                Algorithm = UserDataConstants.BlockAlgorithm,
                NonceBase64 = Convert.ToBase64String(nonceBytes),
                TagBase64 = Convert.ToBase64String(tag),
                CiphertextBase64 = Convert.ToBase64String(ciphertext),
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    private static void EnsureKek(ReadOnlySpan<byte> kek)
    {
        if (kek.Length != UserDataConstants.KekLength)
        {
            throw new ArgumentException($"KEK must be {UserDataConstants.KekLength} bytes.", nameof(kek));
        }
    }

    /// <summary>
    /// Rejects packs whose format or username hash prefix does not match the caller.
    /// Use: High (OpenPack). Scope: UserDataPackCodec.
    /// </summary>
    private static void ValidatePackHeader(UserDataPackWire pack, string username)
    {
        if (pack.Format != UserDataConstants.PackFormat)
        {
            throw new CryptographicException("Unsupported userdata pack format.");
        }

        string expectedPrefix = UserDataUsernameHash.HashPrefix(username);
        if (!string.Equals(pack.UsernameHashPrefix, expectedPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Userdata pack username hash prefix mismatch.");
        }
    }

    /// <summary>
    /// Rejects blocks with missing/unsupported algorithm or empty identity fields.
    /// Use: High (OpenBlock). Scope: UserDataPackCodec.
    /// </summary>
    private static void ValidateBlockHeader(UserDataBlockWire block)
    {
        if (block.Algorithm != UserDataConstants.BlockAlgorithm)
        {
            throw new CryptographicException("Unsupported userdata block algorithm.");
        }

        if (string.IsNullOrWhiteSpace(block.Id) || string.IsNullOrWhiteSpace(block.Type))
        {
            throw new CryptographicException("Userdata block id/type missing.");
        }
    }

    /// <summary>
    /// Base64-decodes a field and requires an exact byte length.
    /// Use: High (OpenBlock). Scope: UserDataPackCodec.
    /// </summary>
    private static byte[] DecodeExact(string base64, int exactLength, string fieldName)
    {
        byte[] bytes = DecodeAtLeast(base64, minLength: exactLength, fieldName);
        if (bytes.Length != exactLength)
        {
            CryptographicOperations.ZeroMemory(bytes);
            throw new CryptographicException($"Invalid userdata block {fieldName} length.");
        }

        return bytes;
    }

    /// <summary>
    /// Base64-decodes a field and requires at least <paramref name="minLength"/> bytes.
    /// Use: High (OpenBlock). Scope: UserDataPackCodec.
    /// </summary>
    private static byte[] DecodeAtLeast(string base64, int minLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new CryptographicException($"Missing userdata block {fieldName}.");
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            if (bytes.Length < minLength)
            {
                CryptographicOperations.ZeroMemory(bytes);
                throw new CryptographicException($"Invalid userdata block {fieldName} length.");
            }

            return bytes;
        }
        catch (FormatException ex)
        {
            throw new CryptographicException($"Invalid userdata block {fieldName} encoding.", ex);
        }
    }
}
