// <copyright file="ProductSessionOperation.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>An original lifecycle identity; credential revisions advance independently within it.</summary>
public sealed class ProductSessionOperation
{
    internal ProductSessionOperation(ProductSessionAuthority owner, long epoch, CancellationToken token)
    {
        Owner = owner;
        Epoch = epoch;
        Token = token;
    }

    /// <summary>Cancellation caused by replacement or logical revocation.</summary>
    public CancellationToken Token { get; }

    internal long Epoch { get; }

    internal ProductSessionAuthority Owner { get; }
}
