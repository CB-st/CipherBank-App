// <copyright file="MacCatalystHttpMessageHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;

namespace CipherBank_app.Platforms.MacCatalyst;

/// <inheritdoc cref="IPlatformHttpMessageHandlerFactory" />
public sealed class MacCatalystHttpMessageHandlerFactory : IPlatformHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler() => new MacCatalystCertificatePinningHandler();
}
