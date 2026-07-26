// <copyright file="PinChange.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Why a change-PIN attempt ended the way it did (drives the surfaced message).</summary>
public enum PinChangeStatus
{
    /// <summary>The stored PIN was replaced.</summary>
    Success,

    /// <summary>The new PIN is shorter than <see cref="PinChangeCoordinator.MinPinLength"/>.</summary>
    TooShort,

    /// <summary>The new PIN and its confirmation differ.</summary>
    Mismatch,

    /// <summary>The new PIN equals the one already in use.</summary>
    SameAsCurrent,

    /// <summary>The supplied current PIN failed verification.</summary>
    WrongCurrentPin,

    /// <summary>Too many failed attempts; the PIN gate is temporarily locked.</summary>
    LockedOut,
}

/// <summary>Result of one change-PIN attempt: machine-readable status plus a user-facing message.</summary>
public readonly record struct PinChangeOutcome(PinChangeStatus Status, string Message)
{
    /// <summary>True only for <see cref="PinChangeStatus.Success"/>. Use: High. Scope: caller branch.</summary>
    public bool Succeeded => Status == PinChangeStatus.Success;
}

/// <summary>
/// Owns the change-PIN decision path so the MAUI ViewModel stays a thin binder: validates the requested
/// PIN shape, then verifies-and-replaces through <see cref="IPinService"/>. The custody blob is sealed with
/// a device secret (see <see cref="CustodyService"/>), so a PIN change never re-seals the mnemonic.
/// Use: Low (only when a user opens Change PIN). Scope: one Profile/Change-PIN interaction.
/// </summary>
public sealed class PinChangeCoordinator
{
    /// <summary>Minimum digits for a new PIN — matches SetPin onboarding.</summary>
    public const int MinPinLength = 6;

    /// <summary>Status → message table so the caller never grows an if/else chain over statuses.</summary>
    private static readonly IReadOnlyDictionary<PinChangeStatus, string> Messages =
        new Dictionary<PinChangeStatus, string>
        {
            [PinChangeStatus.Success] = "PIN updated.",
            [PinChangeStatus.TooShort] = $"PIN must be at least {MinPinLength} digits.",
            [PinChangeStatus.Mismatch] = "PINs do not match.",
            [PinChangeStatus.SameAsCurrent] = "New PIN must differ from the current PIN.",
            [PinChangeStatus.WrongCurrentPin] = "Current PIN is incorrect.",
            [PinChangeStatus.LockedOut] = "Too many failed attempts. Try again later.",
        };

    private readonly IPinService _pin;

    public PinChangeCoordinator(IPinService pin) => _pin = pin;

    /// <summary>
    /// Validates the requested change and, when the shape is sound, swaps the stored PIN after verifying
    /// <paramref name="currentPin"/>. Never partially applies: a rejected attempt leaves the old PIN active.
    /// Use: Low (one call per Change-PIN submit). Scope: the calling ViewModel / test.
    /// </summary>
    public async Task<PinChangeOutcome> ChangeAsync(string currentPin, string newPin, string confirmPin)
    {
        PinChangeStatus shape = ValidateShape(currentPin, newPin, confirmPin);
        if (shape != PinChangeStatus.Success)
        {
            return Describe(shape);
        }

        bool changed = await _pin.ChangePinAsync(currentPin, newPin).ConfigureAwait(false);
        return Describe(changed
            ? PinChangeStatus.Success
            : _pin.IsLockedOut ? PinChangeStatus.LockedOut : PinChangeStatus.WrongCurrentPin);
    }

    /// <summary>
    /// Pure shape check (length, confirmation match, reuse) that needs no secure storage, returning
    /// <see cref="PinChangeStatus.Success"/> when the request is worth sending to <see cref="IPinService"/>.
    /// Use: Low (once per submit). Scope: this coordinator.
    /// </summary>
    public static PinChangeStatus ValidateShape(string currentPin, string newPin, string confirmPin)
        => newPin.Length < MinPinLength ? PinChangeStatus.TooShort
            : !string.Equals(newPin, confirmPin, StringComparison.Ordinal) ? PinChangeStatus.Mismatch
            : string.Equals(newPin, currentPin, StringComparison.Ordinal) ? PinChangeStatus.SameAsCurrent
            : PinChangeStatus.Success;

    /// <summary>
    /// Pairs a status with its user-facing message from the dispatch table.
    /// Use: Low (once per submit). Scope: this coordinator.
    /// </summary>
    private static PinChangeOutcome Describe(PinChangeStatus status) => new(status, Messages[status]);
}
