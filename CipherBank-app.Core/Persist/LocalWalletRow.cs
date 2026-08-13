// <copyright file="LocalWalletRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Local wallet index row.</summary>
public sealed record LocalWalletRow(
    string Id,
    string Symbol,
    string? Label,
    string? Address,
    string? Path,
    int AccountIndex,
    string Kind,
    DateTimeOffset CreatedAt);
