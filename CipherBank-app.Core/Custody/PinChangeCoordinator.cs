// <copyright file="PinChangeCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Resources;

namespace CipherBank_app.Custody;

/// <summary>
/// Owns the change-PIN decision path so the MAUI ViewModel stays a thin binder: validates the requested
/// PIN shape, then hands the swap to <see cref="ICustodyService.ChangePinAsync"/>. Going through custody
/// rather than <see cref="IPinService"/> directly is deliberate — custody enforces the device-secret
/// invariant, so no Shell flow can orphan a legacy PIN-derived blob. The blob is never re-sealed.
/// Use: Low (only when a user opens Change PIN). Scope: one Profile/Change-PIN interaction.
/// </summary>
public sealed class PinChangeCoordinator
{
    /// <summary>Minimum digits for a new PIN — matches SetPin onboarding.</summary>
    public static readonly int MinPinLength = 6;

    /// <summary>Status → message table so the caller never grows an if/else chain over statuses.</summary>
    private static readonly Dictionary<PinChangeStatus, string> Messages =
        new Dictionary<PinChangeStatus, string>
        {
            [PinChangeStatus.Success] = Strings.PinChangeSuccess,
            [PinChangeStatus.TooShort] = Strings.PinChangeTooShort(MinPinLength),
            [PinChangeStatus.Mismatch] = Strings.PinChangeMismatch,
            [PinChangeStatus.SameAsCurrent] = Strings.PinChangeSameAsCurrent,
            [PinChangeStatus.WrongCurrentPin] = Strings.PinChangeWrongCurrentPin,
            [PinChangeStatus.LockedOut] = Strings.PinChangeLockedOut,
            [PinChangeStatus.VaultNotReady] = Strings.PinChangeVaultNotReady,
        };

    /// <summary>Custody result → surfaced status, so this class never grows a branch chain over results.</summary>
    private static readonly Dictionary<CustodyPinChangeResult, PinChangeStatus> FromCustody =
        new Dictionary<CustodyPinChangeResult, PinChangeStatus>
        {
            [CustodyPinChangeResult.Changed] = PinChangeStatus.Success,
            [CustodyPinChangeResult.WrongPin] = PinChangeStatus.WrongCurrentPin,
            [CustodyPinChangeResult.LockedOut] = PinChangeStatus.LockedOut,
            [CustodyPinChangeResult.DeviceSecretMissing] = PinChangeStatus.VaultNotReady,
        };

    private readonly ICustodyService _custody;

    public PinChangeCoordinator(ICustodyService custody)
    {
        _custody = custody;
    }

    /// <summary>
    /// Pure shape check (length, confirmation match, reuse) that needs no secure storage, returning
    /// <see cref="PinChangeStatus.Success"/> when the request is worth sending to custody. Accepts nulls
    /// (an unbound Entry surfaces one) and treats a missing new PIN as too short.
    /// Use: Low (once per submit). Scope: this coordinator.
    /// </summary>
    public static PinChangeStatus ValidateShape(string? currentPin, string? newPin, string? confirmPin)
    {
        if ((newPin?.Length ?? 0) < MinPinLength)
        {
            return PinChangeStatus.TooShort;
        }

        if (!string.Equals(newPin, confirmPin, StringComparison.Ordinal))
        {
            return PinChangeStatus.Mismatch;
        }

        return string.Equals(newPin, currentPin, StringComparison.Ordinal)
            ? PinChangeStatus.SameAsCurrent
            : PinChangeStatus.Success;
    }

    /// <summary>
    /// Validates the requested change and, when the shape is sound, asks custody to swap the stored PIN
    /// after verifying <paramref name="currentPin"/>. Never partially applies: a rejected attempt — including
    /// one refused by the device-secret invariant — leaves the old PIN active.
    /// Use: Low (one call per Change-PIN submit). Scope: the calling ViewModel / test.
    /// </summary>
    public async Task<PinChangeOutcome> ChangeAsync(string? currentPin, string? newPin, string? confirmPin)
    {
        PinChangeStatus shape = ValidateShape(currentPin, newPin, confirmPin);
        if (shape != PinChangeStatus.Success)
        {
            return Describe(shape);
        }

        CustodyPinChangeResult result = await _custody
            .ChangePinAsync(currentPin ?? string.Empty, newPin ?? string.Empty)
            .ConfigureAwait(false);
        return Describe(FromCustody[result]);
    }

    /// <summary>
    /// Pairs a status with its user-facing message from the dispatch table.
    /// Use: Low (once per submit). Scope: this coordinator.
    /// </summary>
    private static PinChangeOutcome Describe(PinChangeStatus status) => new(status, Messages[status]);
}
