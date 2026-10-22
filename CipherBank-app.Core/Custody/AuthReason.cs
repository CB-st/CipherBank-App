// <copyright file="AuthReason.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Step-up auth reasons (Cora requireAuth parity).</summary>
public enum AuthReason
{
    /// <summary>Authorizes moving value to an external recipient or merchant.</summary>
    Payment,

    /// <summary>Authorizes converting one held asset into another.</summary>
    Convert,

    /// <summary>Authorizes creation of a point-of-sale payment credential.</summary>
    PosAuthorize,

    /// <summary>Authorizes presenting an already-created point-of-sale credential.</summary>
    PosPresent,

    /// <summary>Authorizes revealing or exporting private recovery material.</summary>
    RevealKeys,

    /// <summary>Authorizes deriving a new wallet address or account path.</summary>
    Derive,

    /// <summary>Authorizes exporting an encrypted recovery backup.</summary>
    BackupExport,
}
