// <copyright file="ChallengePassModuleTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Hybrid;
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
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
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
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
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
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemorySessionChallengeClient client = new InMemorySessionChallengeClient(algo, template);
        TwoStepChallengePassStructure structure = new TwoStepChallengePassStructure(client);

        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));
        string wire = WireEncoding.ToWire(account.PublicKey);

        object body = await structure.BuildSessionOpenBodyAsync(algo, template, account, wire);
        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);

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
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemorySessionChallengeClient client = new InMemorySessionChallengeClient(algo, template);
        TwoStepChallengePassStructure structure = new TwoStepChallengePassStructure(client);

        ChallengePassSuite a1 = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA1Id,
            algo,
            template,
            structure);

        // Alternate suite: same algo/structure, same template instance — proves SetActive swap.
        ChallengePassSuite alt = new ChallengePassSuite("alt-template-v1", algo, template, structure);
        ChallengePassCatalog catalog = new ChallengePassCatalog([a1, alt], a1.SuiteId);

        catalog.ActiveSuiteId.Should().Be(a1.SuiteId);
        catalog.SetActive("alt-template-v1");
        catalog.Active.SuiteId.Should().Be("alt-template-v1");
        catalog.AvailableSuiteIds.Should().HaveCount(2);
        catalog.AvailableSuiteIds.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task Proof_builder_uses_active_suite()
    {
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemorySessionChallengeClient client = new InMemorySessionChallengeClient(algo, template);
        TwoStepChallengePassStructure structure = new TwoStepChallengePassStructure(client);
        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));

        ChallengePassSuite suite = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA1Id,
            algo,
            template,
            structure);
        ChallengePassCatalog catalog = new ChallengePassCatalog([suite], suite.SuiteId);
        ChallengePassSessionProofBuilder builder = new ChallengePassSessionProofBuilder(catalog, new StaticAccountKeySource(account));

        object body = await builder.BuildOpenBodyAsync(CancellationToken.None);
        Assert.IsType<SessionPassDto>(body);
    }

    /// <summary>
    /// Proves StaticAccountKeySource returns key copies so builder ZeroMemory does not brick a second build.
    /// Use: High. Scope: StaticAccountKeySource + ChallengePassSessionProofBuilder.
    /// </summary>
    [Fact]
    public async Task Proof_builder_a1_survives_second_build_after_private_key_wipe()
    {
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemorySessionChallengeClient client = new InMemorySessionChallengeClient(algo, template);
        TwoStepChallengePassStructure structure = new TwoStepChallengePassStructure(client);
        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));

        ChallengePassSuite suite = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA1Id,
            algo,
            template,
            structure);
        ChallengePassCatalog catalog = new ChallengePassCatalog([suite], suite.SuiteId);
        ChallengePassSessionProofBuilder builder = new ChallengePassSessionProofBuilder(catalog, new StaticAccountKeySource(account));

        object first = await builder.BuildOpenBodyAsync(CancellationToken.None);
        object second = await builder.BuildOpenBodyAsync(CancellationToken.None);
        Assert.IsType<SessionPassDto>(first);
        Assert.IsType<SessionPassDto>(second);
    }

    /// <summary>
    /// Constructor copies retained buffers so caller zeroization and Dispose do not alias.
    /// Use: High. Scope: StaticAccountKeySource ownership boundary.
    /// </summary>
    [Fact]
    public void Fixture_copies_ctor_buffers_so_dispose_does_not_wipe_caller()
    {
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));
        byte[] originalPrivate = account.PrivateKey.ToArray();
        HybridPrivateIdentity hybrid = new HybridPrivateIdentity
        {
            X25519PublicKey = [1, 2, 3],
            X25519PrivateKey = [4, 5, 6],
            MlKemPublicKey = [7, 8, 9],
            MlKemPrivateKey = [10, 11, 12],
        };
        byte[] originalKem = hybrid.MlKemPrivateKey.ToArray();

        using (StaticAccountKeySource source = new StaticAccountKeySource(account, hybrid))
        {
            CryptographicOperations.ZeroMemory(account.PrivateKey);
            CryptographicOperations.ZeroMemory(hybrid.MlKemPrivateKey);
            AccountKeyPair fromFixture = source.RequireUnlockedKeyPair(algo);
            fromFixture.PrivateKey.Should().Equal(originalPrivate);
            source.RequireHybridIdentity().MlKemPrivateKey.Should().Equal(originalKem);
        }

        account.PrivateKey.Should().Equal(new byte[originalPrivate.Length]);
        hybrid.MlKemPrivateKey.Should().Equal(new byte[originalKem.Length]);
    }

    /// <summary>
    /// Dispose zeroes only fixture-owned copies; the caller's arrays stay intact.
    /// Use: High. Scope: StaticAccountKeySource Dispose ownership.
    /// </summary>
    [Fact]
    public void Fixture_dispose_does_not_zero_caller_buffers()
    {
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        AccountKeyPair account = algo.DeriveKeyPair(RandomNumberGenerator.GetBytes(32));
        byte[] originalPrivate = account.PrivateKey.ToArray();

        StaticAccountKeySource source = new StaticAccountKeySource(account);
        source.Dispose();

        account.PrivateKey.Should().Equal(originalPrivate);
        Action afterDispose = () => source.RequireUnlockedKeyPair(algo);
        afterDispose.Should().Throw<ObjectDisposedException>();
    }
}
