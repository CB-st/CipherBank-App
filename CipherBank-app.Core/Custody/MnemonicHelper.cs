// <copyright file="MnemonicHelper.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections;
using NBitcoin;

namespace CipherBank_app.Custody;

/// <summary>BIP39 helpers (Cora bip39.ts parity).</summary>
public static class MnemonicHelper
{
    private const int ChecksumDivisor = 33;
    private const int BitsPerByte = 8;
    private const int HighestBitInByte = 7;

    public static string Generate()
    {
        Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
        return mnemonic.ToString();
    }

    /// <summary>
    /// Returns true when the phrase is a valid BIP39 English mnemonic after normalize.
    /// Use: High (create/recover entry). Scope: MnemonicHelper validation.
    /// </summary>
    public static bool Validate(string phrase)
    {
        try
        {
            // NBitcoin's constructor checks words only; the BIP39 checksum is a separate flag.
            return new Mnemonic(Normalize(phrase), Wordlist.English).IsValidChecksum;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
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
    /// NBitcoin 10.0.12 exposes neither <c>Mnemonic.Entropy</c> nor <c>ToEntropy</c>, so this takes
    /// NBitcoin's own word-index bit stream (<see cref="Wordlist.ToBits(int[])"/>, MSB-first per BIP39)
    /// and keeps the ENT prefix: the stream is ENT + CS bits with CS = ENT/32, so CS = totalBits/33.
    /// 12 words: 132 bits = 128 entropy + 4 checksum. 24 words: 264 = 256 + 8.
    /// </summary>
    public static byte[] Entropy(string phrase)
    {
        BitArray bits = Wordlist.ToBits(Parse(phrase).Indices);
        int entropyBits = bits.Length - (bits.Length / ChecksumDivisor);
        byte[] entropy = new byte[entropyBits / BitsPerByte];

        // BitArray.CopyTo packs LSB-first; BIP39 is MSB-first, so place each bit explicitly.
        for (int bit = 0; bit < entropyBits; bit++)
        {
            if (bits[bit])
            {
                entropy[bit / BitsPerByte] |= (byte)(1 << (HighestBitInByte - (bit % BitsPerByte)));
            }
        }

        return entropy;
    }
}
