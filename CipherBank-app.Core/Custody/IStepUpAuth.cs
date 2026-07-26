// <copyright file="IStepUpAuth.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Cora requireAuth — gate sensitive actions with biometrics or PIN.</summary>
public interface IStepUpAuth
{
    Task<bool> RequireAsync(AuthReason reason, CancellationToken ct);
}
