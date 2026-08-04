// <copyright file="MutableUserDataAccountContext.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Test / lab account context with settable username and mnemonic.</summary>
public sealed class MutableUserDataAccountContext : IUserDataAccountContext
{
    public string? Username { get; set; }

    public string? Mnemonic { get; set; }

    public string Preferred2FaMethod { get; set; } = UserDataWireNames.TwoFaEmail;

    /// <inheritdoc />
    public bool TryGetUnlockedMnemonic(out string mnemonic)
    {
        if (string.IsNullOrWhiteSpace(Mnemonic))
        {
            mnemonic = string.Empty;
            return false;
        }

        mnemonic = Mnemonic;
        return true;
    }
}
