// <copyright file="ChallengePassServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.ChallengePass;

/// <summary>DI install for the challenge/pass module (slot-in suites).</summary>
public static class ChallengePassServiceCollectionExtensions
{
    public const string SuiteA1Id = "a1-x25519-chacha-v1";

    public const string SuiteA2Id = "a2-hybrid-pq-channel-v1";

    /// <summary>
    /// Registers A1 (asymmetric seal) and A2 (hybrid PQ key-share → symmetric channel).
    /// Does not replace <c>ISessionProofBuilder</c> — app binds lab vs challenge/pass explicitly.
    /// </summary>
    public static IServiceCollection AddChallengePassModule(
        this IServiceCollection services,
        string activeSuiteId = SuiteA1Id)
    {
        services.AddSingleton<X25519ChaChaSealAlgorithm>();
        services.AddSingleton<ISealAlgorithm>(sp => sp.GetRequiredService<X25519ChaChaSealAlgorithm>());
        services.AddSingleton<ChallengeIdNonceSha256Template>();
        services.AddSingleton<IChallengeTemplate>(sp => sp.GetRequiredService<ChallengeIdNonceSha256Template>());
        services.AddSingleton<IChallengePassStructure>(sp =>
            new TwoStepChallengePassStructure(sp.GetRequiredService<ISessionChallengeClient>()));

        services.AddSingleton(sp =>
            new ChallengePassSuite(
                SuiteA1Id,
                sp.GetRequiredService<X25519ChaChaSealAlgorithm>(),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>(),
                sp.GetRequiredService<IChallengePassStructure>()));

        // A2 — hybrid ML-KEM+X25519 key share, then ChaCha channel for challenge/pass.
        services.AddSingleton<InMemoryPqKeyShareClient>();
        services.AddSingleton<IPqKeyShareClient>(sp => sp.GetRequiredService<InMemoryPqKeyShareClient>());
        services.AddSingleton<IPqChannel, PqSymmetricChannel>();
        services.AddSingleton<IPqChannelChallengeSource>(sp =>
            new InMemoryPqChannelChallengeSource(
                sp.GetRequiredService<InMemoryPqKeyShareClient>(),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>()));
        services.AddSingleton<PqChannelChallengePassStructure>();
        services.AddSingleton(sp =>
        {
            IPqChannel channel = sp.GetRequiredService<IPqChannel>();
            return new ChallengePassSuite(
                SuiteA2Id,
                new ChannelSealAlgorithm(channel),
                sp.GetRequiredService<ChallengeIdNonceSha256Template>(),
                sp.GetRequiredService<PqChannelChallengePassStructure>());
        });

        services.AddSingleton<IChallengePassCatalog>(sp =>
        {
            var suites = sp.GetServices<ChallengePassSuite>().ToList();
            return new ChallengePassCatalog(suites, activeSuiteId);
        });

        services.AddSingleton<ChallengePassSessionProofBuilder>();
        return services;
    }

    /// <summary>Slot-in another named suite (alternate algo/template/structure combination).</summary>
    public static IServiceCollection AddChallengePassSuite(
        this IServiceCollection services,
        string suiteId,
        Func<IServiceProvider, ISealAlgorithm> algorithmFactory,
        Func<IServiceProvider, IChallengeTemplate> templateFactory,
        Func<IServiceProvider, IChallengePassStructure> structureFactory)
    {
        services.AddSingleton(sp =>
            new ChallengePassSuite(
                suiteId,
                algorithmFactory(sp),
                templateFactory(sp),
                structureFactory(sp)));
        return services;
    }
}
