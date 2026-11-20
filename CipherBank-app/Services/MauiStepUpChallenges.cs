// <copyright file="MauiStepUpChallenges.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;

namespace CipherBank_app.Services;

/// <summary>MAUI prompt/biometric adapter for Core step-up.</summary>
public sealed class MauiStepUpChallenges(
    IBiometricService biometrics,
    IDialogService dialogs,
    ISettingsService settings) : IStepUpChallenges
{
    private readonly IBiometricService _biometrics = biometrics;
    private readonly IDialogService _dialogs = dialogs;
    private readonly ISettingsService _settings = settings;

    public bool BiometricsPreferred => _settings.BiometricAuthEnabled;

    public async Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct = default)
    {
        if (!await _biometrics.IsAvailableAsync().ConfigureAwait(false))
        {
            return false;
        }

        return await _biometrics.AuthenticateAsync(prompt).ConfigureAwait(false);
    }

    public Task<string?> PromptForPinAsync(string prompt, CancellationToken ct = default)
        => _dialogs.PromptPasswordAsync("Confirm", prompt + "\nEnter PIN:", "Continue", "Cancel");
}
