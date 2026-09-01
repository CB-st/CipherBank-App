// <copyright file="HarnessFilterContractTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Reflection;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Host-side contracts for Story-wave maps and Appium URI resolution (no emulator).
/// Use: High (CI / Phase 4 DoD). Scope: E2E harness support.
/// </summary>
public class HarnessFilterContractTests
{
    /// <summary>
    /// Executable waves that already have Facts with matching Story traits must discover ≥1 Fact each.
    /// Backlog-only waves (wallets/cards) are allowed to match zero until Facts land.
    /// Use: High. Scope: Trait discovery.
    /// </summary>
    [Theory]
    [InlineData("account")]
    [InlineData("market")]
    [InlineData("fund")]
    [InlineData("pay")]
    public void ExecutableWaves_HaveAtLeastOneMatchingStoryTrait(string wave)
    {
        HashSet<string> traits = CollectStoryTraits();
        IReadOnlyList<string> storyIds = WaveStories.StoryIdsFor(wave);
        storyIds.Should().NotBeEmpty();
        storyIds.Any(traits.Contains).Should().BeTrue(
            $"wave '{wave}' stories [{string.Join(", ", storyIds)}] must match at least one [Trait(\"Story\", …)] Fact");
    }

    /// <summary>
    /// Documents the bash/C# drift surface: account wave string must stay byte-identical to e2e-android.sh.
    /// Use: High. Scope: WaveStories mirror.
    /// </summary>
    [Fact]
    public void AccountWave_MatchesDocumentedBashMap()
    {
        WaveStories.ByName["account"].Should().Be(
            "CB-ACCOUNT-001 CB-ACCOUNT-002 CB-ACCOUNT-PIN-CHANGE US-ONB-03 US-ONB-04");
    }

    /// <summary>
    /// APPIUM_PORT alone must produce http://127.0.0.1:{port}; APPIUM_SERVER_URL wins when set.
    /// Use: High. Scope: AppiumServerUri.
    /// </summary>
    [Fact]
    public void AppiumServerUri_HonorsPortAndExplicitUrl()
    {
        AppiumServerUri.Resolve(serverUrl: null, port: "5000")
            .Should().Be("http://127.0.0.1:5000");
        AppiumServerUri.Resolve(serverUrl: "http://10.0.0.2:4723", port: "5000")
            .Should().Be("http://10.0.0.2:4723");
        AppiumServerUri.Resolve(serverUrl: "  ", port: "4723")
            .Should().Be("http://127.0.0.1:4723");
    }

    /// <summary>
    /// Collects stable Story trait values from executable facts. Use: High. Scope: HarnessFilterContractTests.
    /// </summary>
    private static HashSet<string> CollectStoryTraits()
    {
        var traits = new HashSet<string>(StringComparer.Ordinal);
        foreach (Type type in typeof(HarnessFilterContractTests).Assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (CustomAttributeData attr in method.CustomAttributes)
                {
                    if (attr.AttributeType.Name != "TraitAttribute" || attr.ConstructorArguments.Count < 2)
                    {
                        continue;
                    }

                    string? name = attr.ConstructorArguments[0].Value as string;
                    string? value = attr.ConstructorArguments[1].Value as string;
                    if (name == "Story" && !string.IsNullOrWhiteSpace(value))
                    {
                        traits.Add(value);
                    }
                }
            }
        }

        return traits;
    }
}
