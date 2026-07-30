// <copyright file="SessionProofBuilderTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class SessionProofBuilderTests
{
    [Fact]
    public async Task Lab_builder_posts_device_attestation_stub()
    {
        var body = await new LabSessionProofBuilder().BuildOpenBodyAsync(default);
        Dictionary<string, string> map = Assert.IsType<Dictionary<string, string>>(body);
        Assert.Equal(LabSessionProofBuilder.LabAttestation, map["DEVICE_ATTESTATION"]);
        Assert.DoesNotContain(map.Keys, k => k.Contains("MNEMONIC", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(map.Keys, k => k.Contains("SEED", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(map.Keys, k => k.Contains("PIN", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Challenge_and_pass_dtos_have_no_seed_fields()
    {
        var challengeNames = typeof(SessionChallengeDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var passNames = typeof(SessionPassDto).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Mnemonic", challengeNames);
        Assert.DoesNotContain("Seed", challengeNames);
        Assert.DoesNotContain("Pin", challengeNames);
        Assert.DoesNotContain("Mnemonic", passNames);
        Assert.DoesNotContain("Seed", passNames);
        Assert.Contains("PassCiphertext", passNames);
        Assert.Contains("AccountPublicKey", passNames);
    }
}
