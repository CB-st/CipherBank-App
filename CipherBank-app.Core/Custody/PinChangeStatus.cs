// <copyright file="PinChangeStatus.cs" company="CipherBank">
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

    /// <summary>
    /// Custody refused the change because no device secret exists yet (see
    /// <see cref="CustodyPinChangeResult.DeviceSecretMissing"/>); the old PIN stays active.
    /// </summary>
    VaultNotReady,
}
