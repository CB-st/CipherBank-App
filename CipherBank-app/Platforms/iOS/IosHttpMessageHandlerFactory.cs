// <copyright file="IosHttpMessageHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;

namespace CipherBank_app.Platforms.Ios;

/// <inheritdoc cref="IPlatformHttpMessageHandlerFactory" />
public sealed class IosHttpMessageHandlerFactory : IPlatformHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler() => new IosCertificatePinningHandler();
}
