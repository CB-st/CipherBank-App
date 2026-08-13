// <copyright file="SessionProofMode.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>How <c>POST /v1/session</c> proves device possession.</summary>
public enum SessionProofMode
{
    /// <summary><c>DEVICE_ATTESTATION=lab</c> stub.</summary>
    Lab = 0,

    /// <summary>A1 asymmetric X25519 sealed challenge/pass.</summary>
    ChallengePassA1 = 1,

    /// <summary>A2 hybrid ML-KEM+X25519 key-share, then PQ channel AEAD.</summary>
    ChallengePassA2 = 2,
}
