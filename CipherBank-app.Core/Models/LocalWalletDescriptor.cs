// <copyright file="LocalWalletDescriptor.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>Domain-facing descriptor for one locally managed wallet.</summary>
public sealed record LocalWalletDescriptor(
    string Id,
    AssetSymbol Symbol,
    string? Label,
    string? Address,
    string? Path,
    int AccountIndex,
    string Kind,
    DateTimeOffset CreatedAt);
