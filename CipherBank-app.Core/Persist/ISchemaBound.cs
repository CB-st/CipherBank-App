// <copyright file="ISchemaBound.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Role seam for stores whose consumers may need the persist schema materialized before
/// first use. Compose into a port; do not depend on this seam alone.
/// </summary>
public interface ISchemaBound
{
    /// <summary>
    /// Ensures the persist schema exists before reads or writes.
    /// Use: High (first consumer touch). Scope: composing port's consumers.
    /// </summary>
    Task EnsureSchemaAsync() => EnsureSchemaAsync(CancellationToken.None);

    Task EnsureSchemaAsync(CancellationToken ct);
}
