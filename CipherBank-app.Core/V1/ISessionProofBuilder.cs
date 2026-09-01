// <copyright file="ISessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>
/// Builds the JSON body for <c>POST /v1/session</c>.
/// Lab stub today; challenge/pass implementation will decrypt with custody-derived
/// account key and seal the pass to the API public key — mnemonic never leaves the device.
/// </summary>
public interface ISessionProofBuilder
{
    Task<object> BuildOpenBodyAsync(CancellationToken ct);
}
