// <copyright file="ProductSessionStartResult.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>Product session values consumed by the application session facade.</summary>
public sealed record ProductSessionStartResult(string AccessToken, int LockIdleSeconds);
