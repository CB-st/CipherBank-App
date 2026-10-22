// <copyright file="MnemonicHelper.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
            _ = new Mnemonic(Normalize(phrase), Wordlist.English);
            return true;
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
    /// NBitcoin 8 no longer exposes <c>Mnemonic.Entropy</c>; recover from word indices.
    /// Each English word is an 11-bit index. The mnemonic is ENT entropy bits plus CS
    /// checksum bits, with CS = ENT/32, so checksum length is totalBits/33. The loop
    /// writes only the entropy prefix, most-significant bit first, and stops before
    /// the checksum bits.
    /// </summary>
    public static byte[] Entropy(string phrase)
    {
        int[] indices = Parse(phrase).Indices;

        // Each English word is an 11-bit wordlist index (2048 words = 2^11). The concatenated
        // bits are ENT entropy bits plus a CS checksum, with CS = ENT/32, so CS = totalBits/33.
        // 12 words: 132 bits = 128 entropy + 4 checksum. 24 words: 264 = 256 + 8.
        int totalBits = indices.Length * BitsPerWordIndex;
        int checksumBits = totalBits / ChecksumDivisor;
        int entropyBits = totalBits - checksumBits;
        byte[] entropy = new byte[entropyBits / BitsPerByte];

        int bitPos = 0;
        foreach (int index in indices)
        {
            // Bit order, not byte order: BIP39 defines the bit stream MSB-first, and this
            // loop reads bits with shifts on an int value. Shifts act on the value, not on
            // its in-memory bytes, so the result is the same on little- and big-endian CPUs.
            // No BitConverter / Array.Reverse is needed because no integer is ever
            // serialized to bytes. Walk bit 10 (MSB) down to bit 0 of the word index;
            // bitPos < entropyBits stops before the trailing checksum bits.
            // Example: "abandon" x11 + "about" -> 11 zero indices then index 3, so all 128
            // entropy bits are 0 and the last 4 bits (0b0011) are the checksum.
            for (int i = HighestWordBitIndex; i >= 0 && bitPos < entropyBits; i--)
            {
                if (((index >> i) & 1) == 1)
                {
                    // bitPos / 8 picks the output byte; 7 - (bitPos % 8) picks the bit inside it,
                    // so the first stream bit lands in bit 7 (MSB) of entropy[0].
                    entropy[bitPos / BitsPerByte] |= (byte)(1 << (HighestBitInByte - (bitPos % BitsPerByte)));
                }

                bitPos++;
            }
        }

        return entropy;
    }
}
