// <copyright file="UnsupportedPlatformHttpMessageHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Fails closed when a target has no certificate-pinning implementation.</summary>
public sealed class UnsupportedPlatformHttpMessageHandlerFactory :
    IPlatformHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler() =>
        throw new PlatformNotSupportedException(
            "CipherBank API transport requires a platform certificate-pinning adapter.");
}
