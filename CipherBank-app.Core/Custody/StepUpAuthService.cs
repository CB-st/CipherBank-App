// <copyright file="StepUpAuthService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Resources;

namespace CipherBank_app.Custody;

/// <inheritdoc />
public sealed class StepUpAuthService(IStepUpChallenges challenges, IPinService pin) : IStepUpAuth
{
    public async Task<bool> RequireAsync(AuthReason reason, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string prompt = PromptFor(reason);

        if (challenges.BiometricsPreferred
            && await challenges.TryBiometricsAsync(prompt, ct).ConfigureAwait(false))
        {
            return true;
        }

        string? entered = await challenges.PromptForPinAsync(prompt, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(entered))
        {
            return false;
        }

        return await pin.VerifyPinAsync(entered).ConfigureAwait(false);
    }

    private static string PromptFor(AuthReason reason) => reason switch
    {
        AuthReason.Payment => UserFacingStrings.StepUpPayment,
        AuthReason.Convert => UserFacingStrings.StepUpConvert,
        AuthReason.PosAuthorize => UserFacingStrings.StepUpPosAuthorize,
        AuthReason.PosPresent => UserFacingStrings.StepUpPosPresent,
        AuthReason.RevealKeys => UserFacingStrings.StepUpRevealKeys,
        AuthReason.Derive => UserFacingStrings.StepUpDerive,
        AuthReason.BackupExport => UserFacingStrings.StepUpBackupExport,
        _ => UserFacingStrings.StepUpConfirmAction,
    };
}
