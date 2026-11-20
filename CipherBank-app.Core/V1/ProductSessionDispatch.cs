// <copyright file="ProductSessionDispatch.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Carries the original credential snapshot through either bearer pipeline.</summary>
public static class ProductSessionDispatch
{
    private static readonly HttpRequestOptionsKey<ProductSessionOperation> Identity = new("CipherBank.ProductSession.Identity");
    private static readonly HttpRequestOptionsKey<ProductSessionSnapshot> Credentials = new("CipherBank.ProductSession");

    /// <summary>Pins a request to its original credentials rather than a later replacement.</summary>
    public static void Set(HttpRequestMessage request, ProductSessionSnapshot snapshot)
        => request.Options.Set(Credentials, snapshot);

    /// <summary>Pins even an unauthenticated dispatch to its original lifecycle.</summary>
    public static void SetOperation(HttpRequestMessage request, ProductSessionOperation operation)
        => request.Options.Set(Identity, operation);

    /// <summary>Validates a pinned dispatch without changing route-specific bearer policy.</summary>
    public static void Check(HttpRequestMessage request, ProductSessionAuthority authority, CancellationToken ct)
    {
        if (request.Options.TryGetValue(Identity, out ProductSessionOperation? operation))
        {
            authority.Check(operation, ct);
        }
    }

    /// <summary>Reads at the handler boundary, or uses a snapshot explicitly pinned by the client.</summary>
    public static async Task<ProductSessionSnapshot?> ReadAsync(
        HttpRequestMessage request, ProductSessionAuthority authority, CancellationToken ct)
    {
        if (request.Options.TryGetValue(Credentials, out ProductSessionSnapshot? snapshot))
        {
            authority.Check(snapshot.Operation, ct);
            return snapshot;
        }

        if (request.Options.TryGetValue(Identity, out ProductSessionOperation? pinned))
        {
            authority.Check(pinned, ct);
            return null;
        }

        ProductSessionOperation operation = authority.Capture();
        return await authority.ReadAsync(operation, ct).ConfigureAwait(false);
    }
}
