// <copyright file="AccountKeyPair.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Wire-facing account keypair (private only while unlocked).</summary>
public readonly record struct AccountKeyPair(byte[] PublicKey, byte[] PrivateKey);
