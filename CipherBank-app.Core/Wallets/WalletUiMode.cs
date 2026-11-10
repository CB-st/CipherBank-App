// <copyright file="WalletUiMode.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
