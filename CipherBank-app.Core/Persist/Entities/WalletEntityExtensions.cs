// <copyright file="WalletEntityExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist.Entities;

/// <summary>Maps wallet persistence entities to outward domain descriptors.</summary>
internal static class WalletEntityExtensions
{
    internal static LocalWalletDescriptor ToDescriptor(this WalletEntity entity) =>
        new(
            entity.Id,
            AssetSymbol.Parse(entity.Symbol),
            entity.Label,
            entity.Address,
            entity.Path,
            entity.AccountIndex,
            entity.Kind,
            entity.CreatedAt);
}
