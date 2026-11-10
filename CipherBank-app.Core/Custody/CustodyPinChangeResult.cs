// <copyright file="CustodyPinChangeResult.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Why a custody-level PIN change ended the way it did.</summary>
public enum CustodyPinChangeResult
{
    /// <summary>The stored PIN was replaced; the sealed blob is untouched.</summary>
    Changed,

    /// <summary>The supplied current PIN failed verification.</summary>
    WrongPin,

    /// <summary>Too many failed attempts; the PIN gate is temporarily locked.</summary>
    LockedOut,

    /// <summary>
    /// No device secret exists, so the blob may still be a legacy PIN-derived seal that only
    /// <see cref="ICustodyService.UnlockAsync"/> can migrate. Changing the PIN now would orphan it.
    /// </summary>
    DeviceSecretMissing,
}
