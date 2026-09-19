// <copyright file="PlatformFeatureRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Controls;
using CipherBank_app.Handlers;
using CipherBank_app.Platforms.Android;
using CipherBank_app.Services;

namespace CipherBank_app.Extensions;

/// <summary>Registers Android-owned host capabilities.</summary>
public static class PlatformFeatureRegistration
{
    public static IServiceCollection AddPlatformFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformHttpMessageHandlerFactory, AndroidHttpMessageHandlerFactory>();
        services.AddSingleton<IMotionPreference, AndroidMotionPreference>();
        return services;
    }

    public static IMauiHandlersCollection AddPlatformHandlers(this IMauiHandlersCollection handlers)
    {
        handlers.AddHandler<BlurBackdropView, SimulatedBlurBackdropViewHandler>();
        return handlers;
    }
}
