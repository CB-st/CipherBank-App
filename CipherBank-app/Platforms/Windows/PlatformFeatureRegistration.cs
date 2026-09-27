// <copyright file="PlatformFeatureRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Controls;
using CipherBank_app.Handlers;
using CipherBank_app.Platforms.Windows;
using CipherBank_app.Services;

namespace CipherBank_app.Extensions;

/// <summary>Registers Windows-owned host capabilities.</summary>
public static class PlatformFeatureRegistration
{
    public static IServiceCollection AddPlatformFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformHttpMessageHandlerFactory, WindowsHttpMessageHandlerFactory>();
        services.AddSingleton<IMotionPreference, WindowsMotionPreference>();
        return services;
    }

    public static IMauiHandlersCollection AddPlatformHandlers(this IMauiHandlersCollection handlers)
    {
        handlers.AddHandler<BlurBackdropView, SimulatedBlurBackdropViewHandler>();
        return handlers;
    }
}
