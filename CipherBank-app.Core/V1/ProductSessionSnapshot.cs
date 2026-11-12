// <copyright file="ProductSessionSnapshot.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Credentials and the exact revision authorized for a request.</summary>
public sealed record ProductSessionSnapshot(
    ProductSessionOperation Operation,
    long Revision,
    string Access,
    string Refresh,
    DateTimeOffset Expires);
