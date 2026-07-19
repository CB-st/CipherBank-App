// <copyright file="ChallengePassServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.ChallengePass;

/// <summary>DI install for the challenge/pass module (slot-in suites).</summary>
public static class ChallengePassServiceCollectionExtensions
{
    public const string SuiteA1Id = "a1-x25519-chacha-v1";

    /// <summary>
    /// Registers default A1 slots + catalog. Does not replace <c>ISessionProofBuilder</c> —
    /// app binds lab vs <see cref="ChallengePassSessionProofBuilder"/> explicitly.
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
