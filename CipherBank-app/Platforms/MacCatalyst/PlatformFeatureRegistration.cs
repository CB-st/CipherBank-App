// <copyright file="PlatformFeatureRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Controls;
using CipherBank_app.Handlers;
using CipherBank_app.Platforms.MacCatalyst;
using CipherBank_app.Pos;
using CipherBank_app.Services;

namespace CipherBank_app.Extensions;

/// <summary>Registers Mac Catalyst-owned host capabilities.</summary>
public static class PlatformFeatureRegistration
{
    /// <summary>Registers transport, motion, and NFC adapters. Use: Low (startup). Scope: this platform.</summary>
    public static IServiceCollection AddPlatformFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformHttpMessageHandlerFactory, MacCatalystHttpMessageHandlerFactory>();
        services.AddSingleton<IMotionPreference, AppleMotionPreference>();
        services.AddSingleton<INfcPresentmentService, NullNfcPresentmentService>();
        return services;
    }

    public static IMauiHandlersCollection AddPlatformHandlers(this IMauiHandlersCollection handlers)
    {
        handlers.AddHandler<BlurBackdropView, BlurBackdropViewHandler>();
        return handlers;
    }
}
