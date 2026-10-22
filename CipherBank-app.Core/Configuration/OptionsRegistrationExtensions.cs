// <copyright file="OptionsRegistrationExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Registers required, class-named configuration sections.</summary>
public static class OptionsRegistrationExtensions
{
    /// <summary>Binds the required section named after <typeparamref name="TOptions"/>.</summary>
    /// <typeparam name="TOptions">Options class whose name is the required section key.</typeparam>
    public static OptionsBuilder<TOptions> AddRequiredOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        TOptions optionsMarker)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(optionsMarker);

        // Runtime class name (decision 9), not CipherBankOptions<T>.SectionName:
        // a subclass cannot publish a different key.
        IConfigurationSection section =
            configuration.GetRequiredSection(optionsMarker.GetType().Name);
        return services.AddOptions<TOptions>().Bind(section);
    }

    /// <summary>
    /// Binds sync, persistence, and user-preference sections and validates them on start.
    /// The MAUI host and the Core test root share these validators.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="configuration">Defaults plus any later deployment overrides.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddValidatedPersistenceOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<SyncSchedulerOptions>, SyncSchedulerOptionsValidator>();
        services.AddRequiredOptions(configuration, new SyncSchedulerOptions())
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PersistenceOptions>, PersistenceOptionsValidator>();
        services.AddRequiredOptions(configuration, new PersistenceOptions())
            .ValidateOnStart();
        services.AddSingleton<
            IValidateOptions<UserPreferenceDefaultsOptions>,
            UserPreferenceDefaultsOptionsValidator>();
        services.AddRequiredOptions(configuration, new UserPreferenceDefaultsOptions())
            .ValidateOnStart();
        return services;
    }
}
