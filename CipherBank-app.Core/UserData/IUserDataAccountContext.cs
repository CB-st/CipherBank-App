// <copyright file="IUserDataAccountContext.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Supplies username + unlocked mnemonic for userdata pack sync (Shell binds to AppSession/custody).
/// </summary>
public interface IUserDataAccountContext
{
    /// <summary>Company-readable handle used for ENROLL/CHALLENGE. Null when not signed in.</summary>
    string? Username { get; }

    /// <summary>Preferred 2FA method string (EMAIL / SMS / AUTHENTICATOR).</summary>
    string Preferred2FaMethod { get; }

    /// <summary>
    /// Returns true and the unlocked mnemonic when custody is open.
    /// Use: High (prefs pack sync). Scope: session / shell adapters.
    /// </summary>
    bool TryGetUnlockedMnemonic(out string mnemonic);
}
