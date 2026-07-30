// <copyright file="StepUpAuthService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <inheritdoc />
public sealed class StepUpAuthService : IStepUpAuth
{
    private readonly IStepUpChallenges _challenges;
    private readonly IPinService _pin;

    public StepUpAuthService(IStepUpChallenges challenges, IPinService pin)
    {
        _challenges = challenges;
        _pin = pin;
    }

    public async Task<bool> RequireAsync(AuthReason reason, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var prompt = PromptFor(reason);

        if (_challenges.BiometricsPreferred
            && await _challenges.TryBiometricsAsync(prompt, ct).ConfigureAwait(false))
        {
            return true;
        }

        var entered = await _challenges.PromptForPinAsync(prompt, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(entered))
        {
            return false;
        }

        return await _pin.VerifyPinAsync(entered).ConfigureAwait(false);
    }

    private static string PromptFor(AuthReason reason) => reason switch
    {
        AuthReason.Payment => "Confirm payment",
        AuthReason.Convert => "Confirm conversion",
        AuthReason.PosAuthorize => "Authorize POS session",
        AuthReason.PosPresent => "Present payment credential",
        AuthReason.RevealKeys => "Reveal recovery phrase",
        AuthReason.Derive => "Derive wallet keys",
        AuthReason.BackupExport => "Export recovery file",
        _ => "Confirm action",
    };
}
