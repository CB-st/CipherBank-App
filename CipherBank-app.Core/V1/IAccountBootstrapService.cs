// <copyright file="IAccountBootstrapService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Import server bootstrap contacts/prefs for returning users. Never touches custody.</summary>
public interface IAccountBootstrapService
{
    /// <summary>
    /// Pulls the account bootstrap payload and upserts recipients/prefs locally; best-effort
    /// (failures leave local state untouched). Use: Medium (first unlock per install). Scope:
    /// local persist only.
    /// </summary>
    Task ApplyAsync(CancellationToken ct);
}
