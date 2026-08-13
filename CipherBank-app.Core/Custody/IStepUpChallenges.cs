// <copyright file="IStepUpChallenges.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Platform prompts for step-up (biometrics / PIN entry).</summary>
public interface IStepUpChallenges
{
    bool BiometricsPreferred { get; }

    Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct);

    /// <summary>Biometric prompt for callers with no ambient token. Use: High (step-up). Scope: IStepUpChallenges consumers.</summary>
    Task<bool> TryBiometricsAsync(string prompt) => TryBiometricsAsync(prompt, CancellationToken.None);

    Task<string?> PromptForPinAsync(string prompt, CancellationToken ct);

    /// <summary>PIN prompt for callers with no ambient token. Use: Medium (step-up fallback). Scope: IStepUpChallenges consumers.</summary>
    Task<string?> PromptForPinAsync(string prompt) => PromptForPinAsync(prompt, CancellationToken.None);
}
