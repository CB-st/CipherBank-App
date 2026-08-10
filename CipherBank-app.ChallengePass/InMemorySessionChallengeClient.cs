// <copyright file="InMemorySessionChallengeClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Local challenge issuer for tests / mock mode: seals with A1 algo + template to the account pubkey.
/// Holds an ephemeral API keypair so passes can be verified in-process.
/// </summary>
public sealed class InMemorySessionChallengeClient : ISessionChallengeClient
{
    private const int ChallengeIdHexLength = 16;

    private readonly ISealAlgorithm _algorithm;
    private readonly IChallengeTemplate _template;
    private readonly AccountKeyPair _apiKey;

    public InMemorySessionChallengeClient()
        : this(null, null)
    {
    }

    public InMemorySessionChallengeClient(ISealAlgorithm? algorithm)
        : this(algorithm, null)
    {
    }

    public InMemorySessionChallengeClient(ISealAlgorithm? algorithm, IChallengeTemplate? template)
    {
        _algorithm = algorithm ?? new X25519ChaChaSealAlgorithm();
        _template = template ?? new ChallengeIdNonceSha256Template();
        byte[] seed = RandomNumberGenerator.GetBytes(_algorithm.PrivateKeySize);
        _apiKey = _algorithm.DeriveKeyPair(seed);
        CryptographicOperations.ZeroMemory(seed);
    }

    public string ApiPublicKeyWire => WireEncoding.ToWire(_apiKey.PublicKey);

    public AccountKeyPair ApiKeyPair => _apiKey;

    public Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire)
        => RequestChallengeAsync(accountPublicKeyWire, CancellationToken.None);

    public Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
    {
        string challengeId = "ch_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..ChallengeIdHexLength];
        byte[] nonce = RandomNumberGenerator.GetBytes(_template.MinNonceLength);
        byte[] plaintext = _template.BuildChallengePlaintext(new ChallengeBindContext
        {
            ChallengeId = challengeId,
            Nonce = nonce,
        });
        byte[] accountPk = WireEncoding.FromWire(accountPublicKeyWire);
        byte[] ciphertext = _algorithm.Seal(plaintext, accountPk);

        return Task.FromResult(new SessionChallengeDto
        {
            ChallengeId = challengeId,
            Ciphertext = WireEncoding.ToWire(ciphertext),
            ApiPublicKey = ApiPublicKeyWire,
            ApiKeyId = "api_lab_1",
            Algorithm = _algorithm.AlgorithmId,
        });
    }

    /// <summary>Verify a device pass against this issuer's API private key (test helper).</summary>
    public bool TryVerifyPass(SessionPassDto pass, out byte[]? passPayload)
    {
        passPayload = null;
        try
        {
            byte[] cipher = WireEncoding.FromWire(pass.PassCiphertext);
            passPayload = _algorithm.Open(cipher, _apiKey.PrivateKey);
            return passPayload.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
