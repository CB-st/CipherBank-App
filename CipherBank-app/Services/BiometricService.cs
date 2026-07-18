// <copyright file="BiometricService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Device biometric / credential gate.</summary>
public interface IBiometricService
{
    Task<bool> IsAvailableAsync();

    Task<bool> AuthenticateAsync(string reason);
}

/// <summary>Essentials-based biometric stub (PIN fallback remains primary).</summary>
public sealed class BiometricService : IBiometricService
{
    public Task<bool> IsAvailableAsync()
        => Task.FromResult(false);

    public Task<bool> AuthenticateAsync(string reason)
        => Task.FromResult(false);
}
