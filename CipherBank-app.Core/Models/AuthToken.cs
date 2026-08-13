// <copyright file="AuthToken.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents an authentication token with access and refresh tokens.
/// </summary>
public record AuthToken(string AccessToken, string RefreshToken, DateTimeOffset ExpiresUtc);
