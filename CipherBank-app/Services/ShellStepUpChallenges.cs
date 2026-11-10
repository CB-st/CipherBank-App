// <copyright file="ShellStepUpChallenges.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;

namespace CipherBank_app.Services;

/// <summary>
/// Step-up prompts on the current shell page. This slice has no biometric API, so the PIN
/// prompt is the only challenge; a missing page fails closed.
/// </summary>
public sealed class ShellStepUpChallenges : IStepUpChallenges
{
    /// <inheritdoc />
    public bool BiometricsPreferred => false;

    /// <inheritdoc />
    public Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<string?> PromptForPinAsync(string prompt, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(prompt);
        ct.ThrowIfCancellationRequested();
        Page? page = Shell.Current?.CurrentPage;
        if (page is null)
        {
            return null;
        }

        return await page.DisplayPromptAsync(
            "Confirm",
            prompt,
            keyboard: Keyboard.Numeric);
    }
}
