// <copyright file="PinService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.Custody;

/// <summary>PIN hash + lockout (Cora pinStore parity).</summary>
public interface IPinService
{
    Task SetPinAsync(string pin);

    Task<bool> VerifyPinAsync(string pin);

    /// <summary>
    /// Replaces the stored PIN after verifying <paramref name="oldPin"/>; returns false (leaving the old
    /// PIN active) when verification fails or the gate is locked out. The custody blob is keyed by a device
    /// secret, so no re-seal is needed. Use: Low (user-initiated PIN change). Scope: secure-store PIN record.
    /// </summary>
    Task<bool> ChangePinAsync(string oldPin, string newPin);

    Task<bool> HasPinAsync();

    /// <summary>Loads lockout / fail counters from secure storage into in-memory fields.</summary>
    Task RefreshAsync();

    int FailedAttempts { get; }

    bool IsLockedOut { get; }

    TimeSpan? LockoutRemaining { get; }
}

/// <inheritdoc />
public sealed class PinService : IPinService
{
    private const string HashKey = "cb_pin_hash";
    private const string SaltKey = "cb_pin_salt";
    private const string FailKey = "cb_pin_fails";
    private const string LockKey = "cb_pin_lock_until";
    private const int MaxFails = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private readonly ISecureStore _store;

    public PinService(ISecureStore store) => _store = store;

    public int FailedAttempts { get; private set; }

    public bool IsLockedOut => LockoutRemaining is { } r && r > TimeSpan.Zero;

    public TimeSpan? LockoutRemaining
    {
        get
        {
            // populated async via RefreshAsync — sync helpers use cached fields
            return _lockUntilUtc is DateTimeOffset until && until > DateTimeOffset.UtcNow
                ? until - DateTimeOffset.UtcNow
                : null;
        }
    }

    private DateTimeOffset? _lockUntilUtc;

    public async Task SetPinAsync(string pin)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        string hash = HashPin(pin, salt);
        await _store.SetAsync(SaltKey, Convert.ToBase64String(salt)).ConfigureAwait(false);
        await _store.SetAsync(HashKey, hash).ConfigureAwait(false);
        await _store.SetAsync(FailKey, "0").ConfigureAwait(false);
        await _store.RemoveAsync(LockKey).ConfigureAwait(false);
        FailedAttempts = 0;
        _lockUntilUtc = null;
    }

    public async Task<bool> HasPinAsync()
        => !string.IsNullOrEmpty(await _store.GetAsync(HashKey).ConfigureAwait(false));

    /// <summary>
    /// Verify-then-replace: only a caller that proves the current PIN can arm a new one, and the failed-attempt
    /// / lockout counters from <see cref="VerifyPinAsync"/> apply here too.
    /// Use: Low (user-initiated PIN change). Scope: secure-store PIN record.
    /// </summary>
    public async Task<bool> ChangePinAsync(string oldPin, string newPin)
    {
        if (!await VerifyPinAsync(oldPin).ConfigureAwait(false))
        {
            return false;
        }

        await SetPinAsync(newPin).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        await RefreshAsync().ConfigureAwait(false);
        if (IsLockedOut)
        {
            return false;
        }

        string? saltB64 = await _store.GetAsync(SaltKey).ConfigureAwait(false);
        string? hash = await _store.GetAsync(HashKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(saltB64) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(saltB64);
        string attempt = HashPin(pin, salt);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(attempt),
                Encoding.UTF8.GetBytes(hash)))
        {
            FailedAttempts = 0;
            await _store.SetAsync(FailKey, "0").ConfigureAwait(false);
            await _store.RemoveAsync(LockKey).ConfigureAwait(false);
            _lockUntilUtc = null;
            return true;
        }

        FailedAttempts++;
        await _store.SetAsync(FailKey, FailedAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
        if (FailedAttempts >= MaxFails)
        {
            _lockUntilUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
            await _store.SetAsync(LockKey, _lockUntilUtc.Value.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        return false;
    }

    public Task RefreshAsync() => RefreshLockAsync();

    private async Task RefreshLockAsync()
    {
        string? fails = await _store.GetAsync(FailKey).ConfigureAwait(false);
        FailedAttempts = int.TryParse(fails, out int f) ? f : 0;
        string? lockMs = await _store.GetAsync(LockKey).ConfigureAwait(false);
        if (long.TryParse(lockMs, out long ms))
        {
            _lockUntilUtc = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            if (_lockUntilUtc <= DateTimeOffset.UtcNow)
            {
                _lockUntilUtc = null;
                FailedAttempts = 0;
                await _store.RemoveAsync(LockKey).ConfigureAwait(false);
                await _store.SetAsync(FailKey, "0").ConfigureAwait(false);
            }
        }
    }

    private static string HashPin(string pin, byte[] salt)
    {
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            120_000,
            HashAlgorithmName.SHA256,
            32);
        return Convert.ToBase64String(hash);
    }
}
