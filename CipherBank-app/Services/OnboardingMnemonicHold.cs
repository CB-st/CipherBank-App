// <copyright file="OnboardingMnemonicHold.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Process-scoped hold for the onboarding/recovery mnemonic between Keys → BackupQuiz → SetPin
/// (and Restore → SetPin) so Shell routes never carry the phrase in a query string.
/// </summary>
public sealed class OnboardingMnemonicHold : IDisposable
{
    private readonly object _gate = new();
    private char[]? _mnemonic;

    /// <summary>
    /// Stores the live mnemonic for the next onboarding page.
    /// Use: High (Keys / Backup / Restore continue). Scope: onboarding handoff.
    /// </summary>
    public void Set(string mnemonic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mnemonic);
        lock (_gate)
        {
            ClearUnlocked();
            _mnemonic = mnemonic.ToCharArray();
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
            return _mnemonic is null ? null : new string(_mnemonic);
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
            ClearUnlocked();
        }
    }

    /// <summary>
    /// Clears retained onboarding material when the application container shuts down.
    /// Use: Low (container shutdown). Scope: singleton onboarding handoff.
    /// </summary>
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Zeroes the currently retained character buffer while the caller holds the gate.
    /// Use: Medium (replace, clear, dispose). Scope: singleton onboarding handoff.
    /// </summary>
    private void ClearUnlocked()
    {
        if (_mnemonic is not null)
        {
            Array.Clear(_mnemonic);
            _mnemonic = null;
        }
    }
}
