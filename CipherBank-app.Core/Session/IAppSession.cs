// <copyright file="IAppSession.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>App-level session: custody unlock + product tokens + idle lock.</summary>
public interface IAppSession
{
    event EventHandler? Locked;

    bool IsBooting { get; }

    bool HasWallet { get; }

    bool IsUnlocked { get; }

    int IdleMs { get; set; }

    string? AccessToken { get; }

    Task BootAsync();

    Task<bool> UnlockAsync(string pin);

    /// <summary>Unlock after successful OS biometrics (device-secret path).</summary>
    Task<bool> UnlockWithDeviceOwnerAsync();

    Task<bool> CanUnlockWithDeviceOwnerAsync();

    void Touch();

    void Lock();

    Task FinishCustodySetupAsync(string mnemonic, string pin);

    /// <summary>Returns true if idle exceeded and lock was applied.</summary>
    bool CheckIdleAndMaybeLock();
}
