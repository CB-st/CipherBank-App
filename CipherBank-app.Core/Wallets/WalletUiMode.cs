// <copyright file="WalletUiMode.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>UI create mode for a wallet module.</summary>
public enum WalletUiMode
{
    Derive,
    Watch,
    Managed,
    Unmanaged,
}
