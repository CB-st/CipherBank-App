// <copyright file="LabSessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Current stub: <c>{ DEVICE_ATTESTATION: "lab" }</c>.</summary>
public sealed class LabSessionProofBuilder : ISessionProofBuilder
{
    public static readonly string LabAttestation = "lab";

    public Task<object> BuildOpenBodyAsync(CancellationToken ct)
        => Task.FromResult<object>(new Dictionary<string, string>
        {
            ["DEVICE_ATTESTATION"] = LabAttestation,
        });
}
