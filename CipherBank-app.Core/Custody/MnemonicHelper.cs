// <copyright file="MnemonicHelper.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using NBitcoin;

namespace CipherBank_app.Custody;

/// <summary>BIP39 helpers (Cora bip39.ts parity).</summary>
public static class MnemonicHelper
{
    private const int BitsPerWordIndex = 11;
    private const int ChecksumDivisor = 33;
    private const int BitsPerByte = 8;
    private const int HighestBitInByte = 7;
    private const int HighestWordBitIndex = 10;

    public static string Generate()
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
        return mnemonic.ToString();
    }

    public static bool Validate(string phrase)
    {
        try
        {
            _ = new Mnemonic(Normalize(phrase), Wordlist.English);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Normalize(string phrase)
        => string.Join(' ', phrase.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)); // NOSONAR (S4040) — BIP39 English wordlist is lowercase-only

    public static string[] Words(string phrase)
        => Normalize(phrase).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static Mnemonic Parse(string phrase)
        => new Mnemonic(Normalize(phrase), Wordlist.English);

    /// <summary>
    /// BIP39 entropy bytes for HKDF account / hybrid key derivation.
    /// NBitcoin 8 no longer exposes <c>Mnemonic.Entropy</c>; recover from word indices.
    /// </summary>
    public static byte[] Entropy(string phrase)
    {
        var indices = Parse(phrase).Indices;
        var totalBits = indices.Length * BitsPerWordIndex;
        var checksumBits = totalBits / ChecksumDivisor;
        var entropyBits = totalBits - checksumBits;
        var entropy = new byte[entropyBits / BitsPerByte];

        var bitPos = 0;
        foreach (var index in indices)
        {
            for (var i = HighestWordBitIndex; i >= 0 && bitPos < entropyBits; i--)
            {
                if (((index >> i) & 1) == 1)
                {
                    entropy[bitPos / BitsPerByte] |= (byte)(1 << (HighestBitInByte - (bitPos % BitsPerByte)));
                }

                bitPos++;
            }
        }

        return entropy;
    }
}
