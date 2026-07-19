// <copyright file="ChallengePassServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.ChallengePass;

/// <summary>DI install for the challenge/pass module (slot-in suites).</summary>
public static class ChallengePassServiceCollectionExtensions
{
    public const string SuiteA1Id = "a1-x25519-chacha-v1";

    public const string SuiteA2Id = "a2-hybrid-pq-channel-v1";

    /// <summary>
    /// Registers A1 + A2 suite machinery. Callers must also register:
    /// <see cref="ISessionChallengeClient"/>, <see cref="IPqKeyShareClient"/>,
    /// <see cref="IPqChannelChallengeSource"/>, <see cref="IAccountKeySource"/>,
    /// and bind <see cref="ISessionProofBuilder"/>.
    /// </summary>
    public static IServiceCollection AddChallengePassModule(
        this IServiceCollection services,
        string activeSuiteId = SuiteA1Id)
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

        services.AddSingleton<PqChannelChallengePassStructure>();
        services.AddSingleton(sp =>
            new ChallengePassSuite(
                SuiteA2Id,
                new ChannelSealAlgorithm(sp.GetRequiredService<IPqChannel>()),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>(),
                sp.GetRequiredService<PqChannelChallengePassStructure>()));

        services.AddSingleton<IChallengePassCatalog>(sp =>
        {
            var suites = sp.GetServices<ChallengePassSuite>().ToList();
            return new ChallengePassCatalog(suites, activeSuiteId);
        });

        return services;
    }
}
