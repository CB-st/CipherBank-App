// <copyright file="ChallengePassServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Configuration;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.Custody;
using CipherBank_app.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.ChallengePass;

/// <summary>DI install for the challenge/pass module (slot-in suites).</summary>
public static class ChallengePassServiceCollectionExtensions
{
    public static string SuiteA1Id => "a1-x25519-chacha-v1";

    public static string SuiteA2Id => "a2-hybrid-pq-channel-v1";

    /// <summary>
    /// Registers A1 + A2 suite machinery with A1 as the active suite. Callers must also register:
    /// <see cref="ISessionChallengeClient"/>, <see cref="IPqKeyShareClient"/>,
    /// <see cref="IPqChannelChallengeSource"/>, <see cref="IAccountKeySource"/>,
    /// and bind <see cref="ISessionProofBuilder"/>.
    /// </summary>
    public static IServiceCollection AddChallengePassModule(this IServiceCollection services)
        => services.AddChallengePassModule(new ChallengePassOptions());

    /// <summary>
    /// Registers A1 + A2 suite machinery. Callers must also register:
    /// <see cref="ISessionChallengeClient"/>, <see cref="IPqKeyShareClient"/>,
    /// <see cref="IPqChannelChallengeSource"/>, <see cref="IAccountKeySource"/>,
    /// and bind <see cref="ISessionProofBuilder"/>.
    /// </summary>
    public static IServiceCollection AddChallengePassModule(
        this IServiceCollection services,
        string activeSuiteId)
        => services.AddChallengePassModule(new ChallengePassOptions { ActiveSuiteId = activeSuiteId });

    /// <summary>
    /// Binds and validates the ChallengePass configuration before the catalog is first resolved.
    /// </summary>
    public static IServiceCollection AddChallengePassModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ChallengePassOptions>()
            .Bind(configuration.GetSection(ChallengePassOptions.SectionName))
            .Validate(static options => options.IsValid(), ChallengePassValidationMessages.ActiveSuiteNotInstalled)
            .ValidateOnStart();

        return AddChallengePassServices(
            services,
            provider => provider.GetRequiredService<IOptions<ChallengePassOptions>>().Value.ActiveSuiteId);
    }

    private static IServiceCollection AddChallengePassModule(
        this IServiceCollection services,
        ChallengePassOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
        {
            throw new ArgumentException(ChallengePassValidationMessages.ActiveSuiteNotInstalled, nameof(options));
        }

        ChallengePassOptions snapshot = new ChallengePassOptions { ActiveSuiteId = options.ActiveSuiteId };
        services.AddSingleton<IOptions<ChallengePassOptions>>(Options.Create(snapshot));
        return AddChallengePassServices(services, _ => snapshot.ActiveSuiteId);
    }

    private static IServiceCollection AddChallengePassServices(
        IServiceCollection services,
        Func<IServiceProvider, string> activeSuiteId)
    {
        services.AddSingleton<X25519ChaChaSealAlgorithm>();
        services.AddSingleton<ChallengeIdNonceSha256Template>();
        services.AddSingleton<IChallengeTemplate>(sp => sp.GetRequiredService<ChallengeIdNonceSha256Template>());
        services.AddSingleton<IPqChannel, PqSymmetricChannel>();
        services.AddSingleton<LabSessionProofBuilder>();
        services.AddSingleton<ChallengePassSessionProofBuilder>();

        services.AddSingleton(sp =>
            new ChallengePassSuite(
                SuiteA1Id,
                sp.GetRequiredService<X25519ChaChaSealAlgorithm>(),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>(),
                new TwoStepChallengePassStructure(sp.GetRequiredService<ISessionChallengeClient>())));

        services.AddSingleton(sp =>
        {
            PqChannelChallengePassStructure structure =
                ActivatorUtilities.CreateInstance<PqChannelChallengePassStructure>(sp);

            // Wipe A2 hybrid identity on every custody lock / expiry / unlock-rollback path.
            sp.GetRequiredService<ICustodyService>().Locked += (_, _) => structure.ClearDeviceIdentity();
            return structure;
        });
        services.AddSingleton(sp =>
            new ChallengePassSuite(
                SuiteA2Id,
                new ChannelSealAlgorithm(sp.GetRequiredService<IPqChannel>()),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>(),
                sp.GetRequiredService<PqChannelChallengePassStructure>()));

        services.AddSingleton<IChallengePassCatalog>(sp =>
        {
            List<ChallengePassSuite> suites = sp.GetServices<ChallengePassSuite>().ToList();
            return new ChallengePassCatalog(suites, activeSuiteId(sp));
        });

        return services;
    }
}
