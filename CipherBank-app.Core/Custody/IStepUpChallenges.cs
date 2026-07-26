// <copyright file="IStepUpChallenges.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Platform prompts for step-up (biometrics / PIN entry).</summary>
public interface IStepUpChallenges
{
    bool BiometricsPreferred { get; }

    Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct);

    Task<string?> PromptForPinAsync(string prompt, CancellationToken ct);
}
