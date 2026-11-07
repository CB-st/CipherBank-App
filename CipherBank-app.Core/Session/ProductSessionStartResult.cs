// <copyright file="ProductSessionStartResult.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>Product session values consumed by the application session facade.</summary>
public sealed record ProductSessionStartResult(string AccessToken, int LockIdleSeconds);
