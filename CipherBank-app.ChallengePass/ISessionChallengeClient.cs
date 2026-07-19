// <copyright file="ISessionChallengeClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Port for structure slot → product API. Implemented by app/mock without baking HTTP into the module.
/// </summary>
public interface ISessionChallengeClient
{
    Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct = default);
}
