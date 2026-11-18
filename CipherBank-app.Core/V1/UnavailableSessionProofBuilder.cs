// <copyright file="UnavailableSessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Blocks session establishment until the host supplies custody-backed protocol proof.</summary>
public sealed class UnavailableSessionProofBuilder : ISessionProofBuilder
{
    /// <inheritdoc />
    public Task<object> BuildOpenBodyAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Custody-backed session proof is unavailable.");
    }
}
