// <copyright file="AuthReason.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Step-up auth reasons (Cora requireAuth parity).</summary>
public enum AuthReason
{
    Payment,
    Convert,
    PosAuthorize,
    PosPresent,
    RevealKeys,
    Derive,
}
