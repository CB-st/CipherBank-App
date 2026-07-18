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
}
