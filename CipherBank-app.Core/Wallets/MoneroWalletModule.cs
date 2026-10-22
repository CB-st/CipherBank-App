// <copyright file="MoneroWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Numerics;
using CipherBank_app.Models;
using Nethereum.Util;

namespace CipherBank_app.Wallets;

/// <summary>Server-managed Monero module. Native derive stays deferred.</summary>
public sealed class MoneroWalletModule : IWalletModule
{
    private const int AddressMinLength = 95;
    private const int AddressMaxLength = 106;

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

    private static bool HasExpectedLength(string address, byte[] decoded) => decoded[0] switch
    {
        18 or 42 or 53 or 63 or 24 or 36 => address.Length == 95 && decoded.Length == 69,
        19 or 54 or 25 => address.Length == 106 && decoded.Length == 77,
        _ => false,
    };

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
