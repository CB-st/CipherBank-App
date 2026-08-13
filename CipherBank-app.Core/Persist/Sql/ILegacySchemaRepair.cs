// <copyright file="ILegacySchemaRepair.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist.Sql;

/// <summary>
/// Compatibility repair for pre-EF on-device SQLite shapes that EF cannot infer.
/// Use: Low (first open after upgrade). Scope: LocalDb initialization.
/// </summary>
internal interface ILegacySchemaRepair
{
    /// <summary>
    /// Creates EF model tables that <c>EnsureCreated</c> skipped because a legacy nonempty DB already existed.
    /// </summary>
    Task EnsureMissingModelTablesAsync(DbContext context, CancellationToken ct);

    /// <summary>
    /// Inspects historical recipient columns, backfills display masks, and scrubs cleartext.
    /// </summary>
    Task ApplyCompatibilityAsync(DbConnection connection, CancellationToken ct);
}
