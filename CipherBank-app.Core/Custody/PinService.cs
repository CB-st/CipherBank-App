// <copyright file="PinService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.Custody;

/// <inheritdoc />
public sealed class PinService : IPinService
{
    private const string HashKey = "cb_pin_hash";
    private const string SaltKey = "cb_pin_salt";
    private const string StagingHashKey = "cb_pin_hash_staging";
    private const string StagingSaltKey = "cb_pin_salt_staging";
    private const string FailKey = "cb_pin_fails";
    private const string LockKey = "cb_pin_lock_until";
    private const int MaxFails = 5;
    private const int MinimumPinLength = 6;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private readonly ISecureStore _store;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset? _lockUntilUtc;

    public PinService(ISecureStore store)
        : this(store, TimeProvider.System)
    {
    }

    public PinService(ISecureStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public int FailedAttempts { get; private set; }

    public bool IsLockedOut => LockoutRemaining is { } r && r > TimeSpan.Zero;

    public TimeSpan? LockoutRemaining
    {
        get
        {
            // populated async via RefreshAsync — sync helpers use cached fields
            return _lockUntilUtc is DateTimeOffset until && until > _timeProvider.GetUtcNow()
                ? until - _timeProvider.GetUtcNow()
                : null;
        }
    }

    public Task SetPinAsync(string pin)
    {
        ArgumentNullException.ThrowIfNull(pin);
        if (pin.Length < MinimumPinLength)
        {
            throw new ArgumentException(
                $"PIN must be at least {MinimumPinLength} characters.",
                nameof(pin));
        }

        return SetPinValidatedAsync(pin);
    }

    private async Task SetPinValidatedAsync(string pin)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        string hash = HashPin(pin, salt);
        string saltB64 = Convert.ToBase64String(salt);

        // Stage the full pair first so a torn promote can still recover a matching salt+hash.
        await _store.SetAsync(StagingSaltKey, saltB64).ConfigureAwait(false);
        await _store.SetAsync(StagingHashKey, hash).ConfigureAwait(false);
        await _store.SetAsync(SaltKey, saltB64).ConfigureAwait(false);
        await _store.SetAsync(HashKey, hash).ConfigureAwait(false);
        await _store.RemoveAsync(StagingSaltKey).ConfigureAwait(false);
        await _store.RemoveAsync(StagingHashKey).ConfigureAwait(false);

        await _store.SetAsync(FailKey, "0").ConfigureAwait(false);
        await _store.RemoveAsync(LockKey).ConfigureAwait(false);
        FailedAttempts = 0;
        _lockUntilUtc = null;
    }

    public async Task<bool> HasPinAsync()
    {
        await RecoverInterruptedPinWriteAsync().ConfigureAwait(false);
        return !string.IsNullOrEmpty(await _store.GetAsync(HashKey).ConfigureAwait(false));
    }

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

        await RecoverInterruptedPinWriteAsync().ConfigureAwait(false);

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
            _lockUntilUtc = _timeProvider.GetUtcNow().Add(LockoutDuration);
            await _store.SetAsync(LockKey, _lockUntilUtc.Value.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        return false;
    }

    public Task RefreshAsync() => RefreshLockAsync();

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

    /// <summary>
    /// Promotes a complete staged salt+hash pair left by an interrupted <see cref="SetPinAsync"/>.
    /// Use: High (Verify/HasPin). Scope: secure-store PIN record recovery.
    /// </summary>
    private async Task RecoverInterruptedPinWriteAsync()
    {
        string? stagedSalt = await _store.GetAsync(StagingSaltKey).ConfigureAwait(false);
        string? stagedHash = await _store.GetAsync(StagingHashKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(stagedSalt) || string.IsNullOrEmpty(stagedHash))
        {
            if (!string.IsNullOrEmpty(stagedSalt))
            {
                await _store.RemoveAsync(StagingSaltKey).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(stagedHash))
            {
                await _store.RemoveAsync(StagingHashKey).ConfigureAwait(false);
            }

            return;
        }

        await _store.SetAsync(SaltKey, stagedSalt).ConfigureAwait(false);
        await _store.SetAsync(HashKey, stagedHash).ConfigureAwait(false);
        await _store.RemoveAsync(StagingSaltKey).ConfigureAwait(false);
        await _store.RemoveAsync(StagingHashKey).ConfigureAwait(false);
    }

    private async Task RefreshLockAsync()
    {
        string? fails = await _store.GetAsync(FailKey).ConfigureAwait(false);
        FailedAttempts = int.TryParse(fails, out int f) ? f : 0;
        string? lockMs = await _store.GetAsync(LockKey).ConfigureAwait(false);
        if (long.TryParse(lockMs, out long ms))
        {
            _lockUntilUtc = DateTimeOffset.FromUnixTimeMilliseconds(ms);
            if (_lockUntilUtc <= _timeProvider.GetUtcNow())
            {
                _lockUntilUtc = null;
                FailedAttempts = 0;
                await _store.RemoveAsync(LockKey).ConfigureAwait(false);
                await _store.SetAsync(FailKey, "0").ConfigureAwait(false);
            }
        }
    }
}
