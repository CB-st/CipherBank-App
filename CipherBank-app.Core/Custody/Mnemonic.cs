// <copyright file="Mnemonic.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using NBitcoin;

namespace CipherBank_app.Custody;

/// <summary>BIP39 helpers (Cora bip39.ts parity).</summary>
public static class MnemonicHelper
{
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
        int totalBits = indices.Length * 11;
        int checksumBits = totalBits / 33;
        int entropyBits = totalBits - checksumBits;
        byte[] entropy = new byte[entropyBits / 8];

        int bitPos = 0;
        foreach (int index in indices)
        {
            for (int i = 10; i >= 0 && bitPos < entropyBits; i--)
            {
                if (((index >> i) & 1) == 1)
                {
                    entropy[bitPos / 8] |= (byte)(1 << (7 - (bitPos % 8)));
                }

                bitPos++;
            }
        }

        return entropy;
    }
}
