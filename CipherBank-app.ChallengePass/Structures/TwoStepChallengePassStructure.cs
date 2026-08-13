// <copyright file="TwoStepChallengePassStructure.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass.Structures;

/// <summary>
/// A1 structure slot: request challenge → open → seal pass → POST /session body as <see cref="SessionPassDto"/>.
/// </summary>
public sealed class TwoStepChallengePassStructure : IChallengePassStructure
{
    private readonly ISessionChallengeClient _client;

    public TwoStepChallengePassStructure(ISessionChallengeClient client)
    {
        _client = client;
    }

    public static string StructureIdValue => "two-step-challenge-pass-v1";

    public string StructureId => StructureIdValue;

    public Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire)
        => BuildSessionOpenBodyAsync(algorithm, challengeTemplate, accountKey, accountPublicKeyWire, CancellationToken.None);

    public async Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        CancellationToken ct)
    {
        SessionChallengeDto challenge = await _client.RequestChallengeAsync(accountPublicKeyWire, ct)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(challenge.Algorithm)
            && !challenge.Algorithm.Equals(algorithm.AlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Challenge ALGORITHM '{challenge.Algorithm}' does not match active seal '{algorithm.AlgorithmId}'.");
        }

        byte[] ciphertext = WireEncoding.FromWire(challenge.Ciphertext);
        byte[] plaintext = algorithm.Open(ciphertext, accountKey.PrivateKey);
        ParsedChallenge parsed = challengeTemplate.ParseChallengePlaintext(plaintext);

        if (!parsed.ChallengeId.Equals(challenge.ChallengeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Opened challenge id does not match CHALLENGE_ID.");
        }

        byte[] passPayload = challengeTemplate.BuildPassPayload(parsed);
        byte[] apiPk = WireEncoding.FromWire(challenge.ApiPublicKey);
        byte[] passCipher = algorithm.Seal(passPayload, apiPk);

        return new SessionPassDto
        {
            ChallengeId = challenge.ChallengeId,
            PassCiphertext = WireEncoding.ToWire(passCipher),
            AccountPublicKey = accountPublicKeyWire,
            ApiKeyId = challenge.ApiKeyId,
            Algorithm = algorithm.AlgorithmId,
        };
    }
}
