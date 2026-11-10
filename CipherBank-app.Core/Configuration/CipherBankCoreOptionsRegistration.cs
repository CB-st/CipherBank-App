// <copyright file="CipherBankCoreOptionsRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Animations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Binds and validates typed Core options from configuration.</summary>
internal static class CipherBankCoreOptionsRegistration
{
    /// <summary>
    /// Registers Core options with start-time validation.
    /// Persistence, sync, and preference defaults are bound here because this composition
    /// root is self-contained for Core tests. The MAUI host binds those three through
    /// <c>AddPersistenceFeature</c> and must not also call <c>AddCipherBankCore</c>.
    /// Use: Low (host startup). Scope: Core DI.
    /// </summary>
    internal static void AddCipherBankCoreOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<CryptographyOptions>, CryptographyOptionsValidator>();
        services.AddRequiredOptions(configuration, new CryptographyOptions())
            .ValidateOnStart();
        services.AddValidatedPersistenceOptions(configuration);
        services.AddRequiredOptions(configuration, new CoraOptions());
        services.AddRequiredOptions(configuration, new CarouselLayoutConfig());
    }
}
