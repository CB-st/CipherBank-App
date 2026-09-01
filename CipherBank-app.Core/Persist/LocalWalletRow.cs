// <copyright file="LocalWalletRow.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
