// <copyright file="IStepUpAuth.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Cora requireAuth — gate sensitive actions with biometrics or PIN.</summary>
public interface IStepUpAuth
{
    Task<bool> RequireAsync(AuthReason reason, CancellationToken ct);

    /// <summary>Gates an action for callers with no ambient token. Use: High (every sensitive tap). Scope: IStepUpAuth consumers.</summary>
    Task<bool> RequireAsync(AuthReason reason) => RequireAsync(reason, CancellationToken.None);
}
