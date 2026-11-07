// <copyright file="PlatformFeatureRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services;

namespace CipherBank_app.Extensions;

/// <summary>Fails closed until Tizen secure transport and blur adapters exist.</summary>
public static class PlatformFeatureRegistration
{
    /// <summary>Registers transport, motion, and NFC adapters. Use: Low (startup). Scope: this platform.</summary>
    public static IServiceCollection AddPlatformFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformHttpMessageHandlerFactory, UnsupportedPlatformHttpMessageHandlerFactory>();
        services.AddSingleton<IMotionPreference, DefaultMotionPreference>();
        services.AddSingleton<INfcPresentmentService, NullNfcPresentmentService>();
        return services;
    }

    public static IMauiHandlersCollection AddPlatformHandlers(this IMauiHandlersCollection handlers) =>
        throw new PlatformNotSupportedException(
            "CipherBank Tizen requires platform blur and secure transport adapters.");
}
