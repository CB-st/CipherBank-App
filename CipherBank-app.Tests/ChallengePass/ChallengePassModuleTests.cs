// <copyright file="ChallengePassModuleTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.ChallengePass;

public sealed class ChallengePassModuleTests
{
    [Fact]
    public void Seal_open_round_trip_with_x25519_chacha()
    {
        var algo = new X25519ChaChaSealAlgorithm();
        byte[] seed = RandomNumberGenerator.GetBytes(32);
        AccountKeyPair pair = algo.DeriveKeyPair(seed);
        byte[] plain = "cipherbank-challenge"u8.ToArray();

        byte[] cipher = algo.Seal(plain, pair.PublicKey);
        byte[] opened = algo.Open(cipher, pair.PrivateKey);

        opened.Should().Equal(plain);
        cipher.Should().NotEqual(plain);
    }

    [Fact]
    public void Template_framing_is_challenge_id_null_nonce()
    {
        var template = new ChallengeIdNonceSha256Template();
        byte[] nonce = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        byte[] p = template.BuildChallengePlaintext(new ChallengeBindContext
        {
            ChallengeId = "ch_abc",
            Nonce = nonce,
        });

        ParsedChallenge parsed = template.ParseChallengePlaintext(p);
        parsed.ChallengeId.Should().Be("ch_abc");
        parsed.Nonce.Should().Equal(nonce);
        template.BuildPassPayload(parsed).Should().HaveCount(32);
    }

    [Fact]
    public async Task Two_step_structure_produces_pass_without_seed_fields()
    {
        var algo = new X25519ChaChaSealAlgorithm();
        var template = new ChallengeIdNonceSha256Template();
        var client = new InMemorySessionChallengeClient(algo, template);
        var structure = new TwoStepChallengePassStructure(client);

        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));
        string wire = WireEncoding.ToWire(account.PublicKey);

        object body = await structure.BuildSessionOpenBodyAsync(algo, template, account, wire);
        var pass = Assert.IsType<SessionPassDto>(body);

        pass.ChallengeId.Should().StartWith("ch_");
        pass.AccountPublicKey.Should().Be(wire);
        pass.Algorithm.Should().Be(X25519ChaChaSealAlgorithm.AlgorithmIdValue);
        pass.PassCiphertext.Should().NotBeNullOrWhiteSpace();

        string json = System.Text.Json.JsonSerializer.Serialize(pass).ToLowerInvariant();
        json.Should().NotContain("mnemonic");
        json.Should().NotContain("seed");
        json.Should().NotContain("\"pin\"");

        client.TryVerifyPass(pass, out byte[]? payload).Should().BeTrue();
        payload.Should().HaveCount(32);
    }

    [Fact]
    public void Catalog_swaps_active_suite_slot()
    {
        var algo = new X25519ChaChaSealAlgorithm();
        var template = new ChallengeIdNonceSha256Template();
        var client = new InMemorySessionChallengeClient(algo, template);
        var structure = new TwoStepChallengePassStructure(client);

        var a1 = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA1Id,
            algo,
            template,
            structure);

        // Alternate suite: same algo/structure, same template instance — proves SetActive swap.
        var alt = new ChallengePassSuite("alt-template-v1", algo, template, structure);
        var catalog = new ChallengePassCatalog([a1, alt], a1.SuiteId);

        catalog.ActiveSuiteId.Should().Be(a1.SuiteId);
        catalog.SetActive("alt-template-v1");
        catalog.GetActive().SuiteId.Should().Be("alt-template-v1");
        catalog.AvailableSuiteIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Proof_builder_uses_active_suite()
    {
        var algo = new X25519ChaChaSealAlgorithm();
        var template = new ChallengeIdNonceSha256Template();
        var client = new InMemorySessionChallengeClient(algo, template);
        var structure = new TwoStepChallengePassStructure(client);
        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));

        var suite = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA1Id,
            algo,
            template,
            structure);
        var catalog = new ChallengePassCatalog([suite], suite.SuiteId);
        var builder = new ChallengePassSessionProofBuilder(catalog, new StaticAccountKeySource(account));

        object body = await builder.BuildOpenBodyAsync();
        Assert.IsType<SessionPassDto>(body);
    }
}
