// <copyright file="AddressDerive.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using NBitcoin;
using Nethereum.HdWallet;
using Nethereum.Util;

namespace CipherBank_app.Wallets;

/// <summary>BIP84/BIP44 derivation (Cora derive.ts parity).</summary>
public static class AddressDerive
{
    public static bool IsDerivable(string symbol)
    {
        string s = symbol.ToUpperInvariant();
        return s is "BTC" or "ETH" or "LTC" or "DOGE";
    }

    public static DerivedAddress? Derive(string symbol, string mnemonic)
        => Derive(symbol, mnemonic, 0);

    public static DerivedAddress? Derive(string symbol, string mnemonic, int accountIndex)
    {
        string s = symbol.ToUpperInvariant();
        return s switch
        {
            "BTC" => DeriveBtc(mnemonic, accountIndex),
            "LTC" => DeriveLtc(mnemonic, accountIndex),
            "DOGE" => DeriveDoge(mnemonic, accountIndex),
            "ETH" => DeriveEth(mnemonic, accountIndex),
            _ => null,
        };
    }

    public static DerivedAddress DeriveBtc(string mnemonic)
        => DeriveBtc(mnemonic, 0);

    public static DerivedAddress DeriveBtc(string mnemonic, int accountIndex)
    {
        Mnemonic m = MnemonicHelper.Parse(mnemonic);
        ExtKey root = m.DeriveExtKey();
        string path = $"m/84'/0'/0'/0/{accountIndex}";
        ExtKey key = root.Derive(new KeyPath(path));
        BitcoinAddress addr = key.Neuter().PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
        return new DerivedAddress(addr.ToString(), path, accountIndex);
    }

    public static DerivedAddress DeriveLtc(string mnemonic)
        => DeriveLtc(mnemonic, 0);

    public static DerivedAddress DeriveLtc(string mnemonic, int accountIndex)
    {
        // Litecoin mainnet via NBitcoin Litecoin networks if available; fallback bech32 manually via BTC path style
        Mnemonic m = MnemonicHelper.Parse(mnemonic);
        ExtKey root = m.DeriveExtKey();
        string path = $"m/84'/2'/0'/0/{accountIndex}";
        ExtKey key = root.Derive(new KeyPath(path));

        // Use Litecoin network if registered; otherwise encode wit program with ltc HRP via BTC segwit then rewrite
        WitKeyId wit = key.Neuter().PubKey.WitHash;
        string address = new BitcoinWitPubKeyAddress(wit, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString();
        return new DerivedAddress(address, path, accountIndex);
    }

    public static DerivedAddress DeriveDoge(string mnemonic)
        => DeriveDoge(mnemonic, 0);

    public static DerivedAddress DeriveDoge(string mnemonic, int accountIndex)
    {
        Mnemonic m = MnemonicHelper.Parse(mnemonic);
        ExtKey root = m.DeriveExtKey();
        string path = $"m/44'/3'/0'/0/{accountIndex}";
        ExtKey key = root.Derive(new KeyPath(path));
        BitcoinAddress addr = key.Neuter().PubKey.GetAddress(ScriptPubKeyType.Legacy, NBitcoin.Altcoins.Dogecoin.Instance.Mainnet);
        return new DerivedAddress(addr.ToString(), path, accountIndex);
    }

    public static DerivedAddress DeriveEth(string mnemonic)
        => DeriveEth(mnemonic, 0);

    public static DerivedAddress DeriveEth(string mnemonic, int accountIndex)
    {
        var wallet = new Wallet(MnemonicHelper.Normalize(mnemonic), null);
        Nethereum.Web3.Accounts.Account account = wallet.GetAccount(accountIndex);
        string path = $"m/44'/60'/0'/0/{accountIndex}";
        string checksum = new AddressUtil().ConvertToChecksumAddress(account.Address);
        return new DerivedAddress(checksum, path, accountIndex);
    }
}
