// <copyright file="OnboardingMnemonicHold.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Process-scoped hold for the onboarding/recovery mnemonic between Keys → BackupQuiz → SetPin
/// (and Restore → SetPin) so Shell routes never carry the phrase in a query string.
/// </summary>
public sealed class OnboardingMnemonicHold
{
    private readonly object _gate = new();
    private string? _mnemonic;

    /// <summary>
    /// Stores the live mnemonic for the next onboarding page.
    /// Use: High (Keys / Backup / Restore continue). Scope: onboarding handoff.
    /// </summary>
    public void Set(string mnemonic)
    {
        lock (_gate)
        {
            _mnemonic = mnemonic;
        }
    }

    /// <summary>
    /// Returns the held mnemonic without clearing it (back-navigation safe).
    /// Use: High (BackupQuiz / SetPin appear). Scope: onboarding handoff.
    /// </summary>
    public string? Peek()
    {
        lock (_gate)
        {
            return _mnemonic;
        }
    }

    /// <summary>
    /// Clears the held mnemonic after SetPin seals custody (or on abandon).
    /// Use: High (successful seal). Scope: onboarding handoff.
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            _mnemonic = null;
        }
    }
}
