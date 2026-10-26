// <copyright file="IPlatformHttpMessageHandlerFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Creates the secure primary HTTP handler owned by the active platform.</summary>
public interface IPlatformHttpMessageHandlerFactory
{
    HttpMessageHandler CreateHandler();
}
