// <copyright file="WindowsHttpMessageHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Platforms.Windows;

/// <inheritdoc cref="IPlatformHttpMessageHandlerFactory" />
public sealed class WindowsHttpMessageHandlerFactory(
    ILogger<WindowsCertificatePinningHandler> logger) : IPlatformHttpMessageHandlerFactory
{
    public HttpMessageHandler CreateHandler() => new WindowsCertificatePinningHandler(logger);
}
