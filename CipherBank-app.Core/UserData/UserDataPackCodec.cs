// <copyright file="UserDataPackCodec.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

namespace CipherBank_app.UserData;

/// <summary>
/// Seals/opens cipherbank-userdata-pack-v1 blocks and Base64-encodes the pack as USER_DATA_BLOB.
/// Delegates AEAD to <see cref="IUserDataBlockCipher"/> (default AES-GCM suite slot).
/// </summary>
public static class UserDataPackCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    private static readonly AesGcmUserDataBlockCipher DefaultBlocks = new();

    /// <summary>
    /// Seals plaintext blocks under the KEK and builds a pack envelope for the username.
    /// Use: High (prefs push). Scope: userdata sync.
    /// </summary>
    public static UserDataPackWire SealPack(
        string username,
        uint contentVersion,
        ReadOnlySpan<byte> kek,
        IReadOnlyList<UserDataPlainBlock> plainBlocks)
        => SealPack(username, contentVersion, kek, plainBlocks, DefaultBlocks);

    /// <summary>
    /// Seals a pack with an injected block cipher (suite / tests).
    /// Use: Medium (catalog Active.Blocks). Scope: userdata sync.
    /// </summary>
    public static UserDataPackWire SealPack(
        string username,
        uint contentVersion,
        ReadOnlySpan<byte> kek,
        IReadOnlyList<UserDataPlainBlock> plainBlocks,
        IUserDataBlockCipher blockCipher)
    {
        ArgumentNullException.ThrowIfNull(plainBlocks);
        ArgumentNullException.ThrowIfNull(blockCipher);
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
            pack.Blocks.Add(blockCipher.Seal(plain, kek, usernameHashHex, contentVersion));
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
        => DefaultBlocks.Seal(plain, kek, usernameHashHex, contentVersion);

    /// <summary>
    /// Opens one block; GCM failure throws <see cref="CryptographicException"/>.
    /// Use: High (restore). Scope: UserDataPackCodec.
    /// </summary>
    public static string OpenBlock(
        UserDataBlockWire block,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion)
        => DefaultBlocks.Open(block, kek, usernameHashHex, contentVersion);

    /// <summary>
    /// Opens every block; skips unknown types when <paramref name="skipUnknownTypes"/> is true.
    /// Use: High (GRAB apply). Scope: userdata restore.
    /// </summary>
    public static Dictionary<string, string> OpenPack(
        UserDataPackWire pack,
        string username,
        ReadOnlySpan<byte> kek,
        bool skipUnknownTypes = true)
        => OpenPack(pack, username, kek, DefaultBlocks, skipUnknownTypes);

    /// <summary>
    /// Opens a pack with an injected block cipher. Use: Medium (suite). Scope: userdata restore.
    /// </summary>
    public static Dictionary<string, string> OpenPack(
        UserDataPackWire pack,
        string username,
        ReadOnlySpan<byte> kek,
        IUserDataBlockCipher blockCipher,
        bool skipUnknownTypes = true)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(blockCipher);
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

            opened[block.Id] = blockCipher.Open(block, kek, usernameHashHex, pack.ContentVersion);
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
        => DefaultBlocks.SealWithNonce(plain, kek, usernameHashHex, contentVersion, nonce);

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
}
