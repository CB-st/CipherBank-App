// <copyright file="TwoStepChallengePassStructure.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
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

        byte[]? ciphertext = null;
        byte[]? plaintext = null;
        byte[]? passPayload = null;
        byte[]? apiPublicKey = null;
        byte[]? passCiphertext = null;
        ParsedChallenge? parsed = null;
        try
        {
            ciphertext = WireEncoding.FromWire(challenge.Ciphertext);
            plaintext = algorithm.Open(ciphertext, accountKey.PrivateKey);
            parsed = challengeTemplate.ParseChallengePlaintext(plaintext);
            if (!parsed.ChallengeId.Equals(challenge.ChallengeId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Opened challenge id does not match CHALLENGE_ID.");
            }

            passPayload = challengeTemplate.BuildPassPayload(parsed);
            apiPublicKey = WireEncoding.FromWire(challenge.ApiPublicKey);
            passCiphertext = algorithm.Seal(passPayload, apiPublicKey);
            return new SessionPassDto
            {
                ChallengeId = challenge.ChallengeId,
                PassCiphertext = WireEncoding.ToWire(passCiphertext),
                AccountPublicKey = accountPublicKeyWire,
                ApiKeyId = challenge.ApiKeyId,
                Algorithm = algorithm.AlgorithmId,
            };
        }
        finally
        {
            Zero(ciphertext);
            Zero(plaintext);
            Zero(passPayload);
            Zero(apiPublicKey);
            Zero(passCiphertext);
            if (parsed is not null)
            {
                Zero(parsed.Nonce);
                Zero(parsed.RawPlaintext);
            }
        }
    }

    /// <summary>
    /// Clears an owned intermediate buffer when allocation reached the current stage.
    /// Use: High (every challenge build). Scope: A1 structure.
    /// </summary>
    private static void Zero(byte[]? buffer)
    {
        if (buffer is not null)
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
