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
    private const int HighestWordBitIndex = 10;
    private const int MaxBitIndexInByte = 7;
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
        => string.Join(' ', phrase.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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
        int[] indices = Parse(phrase).Indices;
        int totalBits = indices.Length * BitsPerWordIndex;
        int checksumBits = totalBits / ChecksumDivisor;
        int entropyBits = totalBits - checksumBits;
        byte[] entropy = new byte[entropyBits / BitsPerByte];

        int bitPos = 0;
        foreach (int index in indices)
        {
            for (int i = HighestWordBitIndex; i >= 0 && bitPos < entropyBits; i--)
            {
                if (((index >> i) & 1) == 1)
                {
                    entropy[bitPos / BitsPerByte] |= (byte)(1 << (MaxBitIndexInByte - (bitPos % BitsPerByte)));
                }

                bitPos++;
            }
        }

        return entropy;
    }
}
