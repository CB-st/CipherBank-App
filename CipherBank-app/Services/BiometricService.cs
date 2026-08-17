// <copyright file="BiometricService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Plugin.Maui.Biometric;

namespace CipherBank_app.Services;

/// <summary>
/// Cross-platform biometric gate via Plugin.Maui.Biometric.
/// Logical gate only — custody AES key is the device secret in SecureStorage.
/// Use: Medium (unlock / step-up). Scope: IBiometricService.
/// </summary>
public sealed class BiometricService : IBiometricService
{
    private readonly IBiometric _biometric;

    public BiometricService(IBiometric biometric)
    {
        ArgumentNullException.ThrowIfNull(biometric);
        _biometric = biometric;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            BiometricHwStatus status = await _biometric
                .GetAuthenticationStatusAsync()
                .ConfigureAwait(false);
            return status == BiometricHwStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        try
        {
            AuthenticationRequest request = new AuthenticationRequest
            {
                Title = "CipherBank",
                Subtitle = reason,
                NegativeText = "Cancel",
                AuthStrength = AuthenticatorStrength.Strong,
                AllowPasswordAuth = false,
            };
            AuthenticationResponse result = await _biometric
                .AuthenticateAsync(request, CancellationToken.None)
                .ConfigureAwait(false);
            return result.Status == BiometricResponseStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
