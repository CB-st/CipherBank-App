// <copyright file="ChallengePassDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.ChallengePass.Configuration;

/// <summary>Loads the repository-owned ChallengePass defaults embedded in this module.</summary>
public static class ChallengePassDefaultsConfiguration
{
    private const string ResourceName = "CipherBank_app.ChallengePass.Config.challenge-pass.json";

    /// <summary>Builds the default ChallengePass configuration.</summary>
    public static IConfigurationRoot Build()
    {
        Assembly assembly = typeof(ChallengePassDefaultsConfiguration).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded configuration resource '{ResourceName}'.");
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }
}
