// <copyright file="IBiometricService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Device biometric / credential gate.</summary>
public interface IBiometricService
{
    Task<bool> IsAvailableAsync();

    Task<bool> AuthenticateAsync(string reason);
}
