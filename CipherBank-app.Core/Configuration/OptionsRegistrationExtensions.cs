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
        IConfigurationSection section =
            configuration.GetRequiredSection(optionsMarker.GetType().Name);
        return services.AddOptions<TOptions>().Bind(section);
    }
}
