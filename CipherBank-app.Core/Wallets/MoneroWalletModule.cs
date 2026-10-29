// <copyright file="MoneroWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Frozen;
using System.Numerics;
using CipherBank_app.Models;
using Nethereum.Util;

namespace CipherBank_app.Wallets;

/// <summary>Server-managed Monero module. Native derive stays deferred.</summary>
public sealed class MoneroWalletModule : IWalletModule
{
    private const int AddressMinLength = 95;
    private const int AddressMaxLength = 106;

    // Standard and subaddress payloads: prefix + spend key + view key + checksum.
    private const int StandardDecodedLength = 69;

    // Integrated payloads add an 8-byte payment ID.
    private const int IntegratedDecodedLength = 77;

    private const byte MainnetStandardPrefix = 18;
    private const byte MainnetIntegratedPrefix = 19;
    private const byte MainnetSubaddressPrefix = 42;
    private const byte TestnetStandardPrefix = 53;
    private const byte TestnetIntegratedPrefix = 54;
    private const byte TestnetSubaddressPrefix = 63;
    private const byte StagenetStandardPrefix = 24;
    private const byte StagenetIntegratedPrefix = 25;
    private const byte StagenetSubaddressPrefix = 36;

    private const int ChecksumLength = 4;
    private const int FullBlockChars = 11;
    private const int Base58Radix = 58;
    private const int BitsPerByte = 8;

    // Monero Base58 alphabet (Bitcoin-style; no 0/O/I/l).
    private const string Base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    // Monero Base58 encodes 8-byte blocks as 11 characters; a short final block of
    // N characters decodes to this many bytes (-1 marks a length no block can have).
    private static readonly int[] BlockBytesForChars = [0, -1, 1, 2, -1, 3, 4, 5, -1, 6, 7, 8];

    // Each single-byte network prefix fixes both the encoded and the decoded address length.
    private static readonly FrozenDictionary<byte, (int Chars, int Bytes)> LengthsByPrefix =
        new Dictionary<byte, (int Chars, int Bytes)>
        {
            [MainnetStandardPrefix] = (AddressMinLength, StandardDecodedLength),
            [MainnetSubaddressPrefix] = (AddressMinLength, StandardDecodedLength),
            [TestnetStandardPrefix] = (AddressMinLength, StandardDecodedLength),
            [TestnetSubaddressPrefix] = (AddressMinLength, StandardDecodedLength),
            [StagenetStandardPrefix] = (AddressMinLength, StandardDecodedLength),
            [StagenetSubaddressPrefix] = (AddressMinLength, StandardDecodedLength),
            [MainnetIntegratedPrefix] = (AddressMaxLength, IntegratedDecodedLength),
            [TestnetIntegratedPrefix] = (AddressMaxLength, IntegratedDecodedLength),
            [StagenetIntegratedPrefix] = (AddressMaxLength, IntegratedDecodedLength),
        }.ToFrozenDictionary();

    /// <inheritdoc />
    public AssetSymbol Symbol => AssetSymbol.Xmr;

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } =
        [WalletUiMode.Managed, WalletUiMode.Unmanaged, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => false;

    /// <inheritdoc />
    public bool UsesServerWallets => true;

    /// <inheritdoc />
    public string? Notes => "Hybrid: managed/unmanaged via /wallets API — native derive deferred";

    /// <inheritdoc />
    public bool IsValidAddress(string address)
    {
        if (address.Length is < AddressMinLength or > AddressMaxLength)
        {
            return false;
        }

        // Single-byte network prefixes select standard/subaddress or integrated payloads.
        byte[]? decoded = DecodeBase58(address);
        return decoded is not null
            && HasExpectedLength(address, decoded)
            && HasValidChecksum(decoded);
    }

    /// <inheritdoc />
    public DerivedAddress? TryDerive(string mnemonic, int accountIndex)
    {
        _ = mnemonic;
        _ = accountIndex;
        return null;
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
    {
        _ = label;
        _ = message;
        return PaymentUri.Account("monero", "tx_amount", address, amount);
    }

    private static bool HasExpectedLength(string address, byte[] decoded)
        => LengthsByPrefix.TryGetValue(decoded[0], out (int Chars, int Bytes) expected)
            && address.Length == expected.Chars
            && decoded.Length == expected.Bytes;

    // The last four bytes are the first four bytes of Keccak-256 over everything before them.
    private static bool HasValidChecksum(byte[] decoded)
    {
        int bodyLength = decoded.Length - ChecksumLength;
        byte[] hash = Sha3Keccack.Current.CalculateHash(decoded[..bodyLength]);
        return hash.AsSpan(0, ChecksumLength).SequenceEqual(decoded.AsSpan(bodyLength));
    }

    private static byte[]? DecodeBase58(string address)
    {
        List<byte> output = new(address.Length);
        for (int start = 0; start < address.Length; start += FullBlockChars)
        {
            int chars = Math.Min(FullBlockChars, address.Length - start);
            int byteCount = BlockBytesForChars[chars];
            if (byteCount < 0)
            {
                return null;
            }

            BigInteger value = BigInteger.Zero;
            for (int i = start; i < start + chars; i++)
            {
                int digit = Base58Alphabet.IndexOf(address[i], StringComparison.Ordinal);
                if (digit < 0)
                {
                    return null;
                }

                value = (value * Base58Radix) + digit;
            }

            // A block that overflows its byte width is not a valid encoding.
            if (value >= BigInteger.One << (BitsPerByte * byteCount))
            {
                return null;
            }

            byte[] block = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            output.AddRange(new byte[byteCount - block.Length]);
            output.AddRange(block);
        }

        return [.. output];
    }
}
