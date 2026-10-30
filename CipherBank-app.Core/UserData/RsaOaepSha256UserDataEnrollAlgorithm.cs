// <copyright file="RsaOaepSha256UserDataEnrollAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace CipherBank_app.UserData;

/// <summary>
/// Deterministic RSA-2048 enroll keys from enroll-seed via BouncyCastle DigestRandomGenerator(SHA-256).
/// Challenge encryption: RSAES-OAEP with SHA-256 digest and MGF1-SHA-256 (matches CipherBank-src Encrypter).
/// </summary>
public sealed class RsaOaepSha256UserDataEnrollAlgorithm : IUserDataEnrollAlgorithm
{
    private const int PemBase64LineLength = 64;

    public string AlgorithmId => UserDataConstants.EnrollAlgorithmRsaOaepSha256V1;

    /// <inheritdoc />
    public UserDataEnrollKeyPair DeriveKeyPair(ReadOnlySpan<byte> enrollSeed64)
    {
        if (enrollSeed64.Length != UserDataConstants.EnrollSeedLength)
        {
            throw new ArgumentException(
                $"Enroll seed must be {UserDataConstants.EnrollSeedLength} bytes.",
                nameof(enrollSeed64));
        }

        byte[] seedCopy = enrollSeed64.ToArray();
        try
        {
            SecureRandom random = CreateDeterministicRandom(seedCopy);
            RsaKeyPairGenerator keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new KeyGenerationParameters(random, UserDataConstants.RsaKeySizeBits));
            AsymmetricCipherKeyPair pair = keyGen.GenerateKeyPair();

            SubjectPublicKeyInfo spki = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pair.Public);
            byte[] spkiDer = spki.GetDerEncoded();
            string fingerprint = Convert.ToHexStringLower(SHA256.HashData(spkiDer));
            string publicPem = EncodeSpkiPem(spkiDer);
            byte[] pkcs8 = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private).GetDerEncoded();

            return new UserDataEnrollKeyPair(AlgorithmId, publicPem, fingerprint, pkcs8);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seedCopy);
        }
    }

    /// <inheritdoc />
    public byte[] DecryptChallenge(ReadOnlySpan<byte> encryptedChallenge, UserDataEnrollKeyPair keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (encryptedChallenge.Length == 0)
        {
            throw new CryptographicException("Encrypted challenge is empty.");
        }

        AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(keys.PrivateKeyPkcs8Der.ToArray());
        OaepEncoding engine = CreateOaepEngine();
        engine.Init(forEncryption: false, privateKey);
        return engine.ProcessBlock(encryptedChallenge.ToArray(), 0, encryptedChallenge.Length);
    }

    /// <inheritdoc />
    public byte[] EncryptChallenge(ReadOnlySpan<byte> challengePlaintext, string publicKeyPem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);
        if (challengePlaintext.Length == 0)
        {
            throw new CryptographicException("Challenge plaintext is empty.");
        }

        AsymmetricKeyParameter publicKey = ParsePublicKeyPem(publicKeyPem);
        OaepEncoding engine = CreateOaepEngine();
        engine.Init(forEncryption: true, publicKey);
        byte[] plain = challengePlaintext.ToArray();
        try
        {
            return engine.ProcessBlock(plain, 0, plain.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    /// <summary>
    /// SHA-256 DigestRandomGenerator seeded solely from enroll-seed (no OS entropy).
    /// Use: High (DeriveKeyPair). Scope: RsaOaepSha256UserDataEnrollAlgorithm.
    /// </summary>
    private static SecureRandom CreateDeterministicRandom(byte[] enrollSeed64)
    {
        DigestRandomGenerator digestRandom = new DigestRandomGenerator(new Sha256Digest());
        digestRandom.AddSeedMaterial(enrollSeed64);
        return new SecureRandom(digestRandom);
    }

    /// <summary>
    /// RSAES-OAEP with SHA-256 / MGF1-SHA-256. Use: High (encrypt/decrypt). Scope: enroll algorithm.
    /// </summary>
    private static OaepEncoding CreateOaepEngine()
        => new OaepEncoding(new RsaEngine(), new Sha256Digest(), new Sha256Digest(), encodingParams: null);

    /// <summary>
    /// Formats SPKI DER as PEM PUBLIC KEY. Use: Medium (DeriveKeyPair). Scope: enroll algorithm.
    /// </summary>
    private static string EncodeSpkiPem(byte[] spkiDer)
    {
        string b64 = Convert.ToBase64String(spkiDer);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("-----BEGIN PUBLIC KEY-----");
        for (int i = 0; i < b64.Length; i += PemBase64LineLength)
        {
            int len = Math.Min(PemBase64LineLength, b64.Length - i);
            sb.AppendLine(b64.Substring(i, len));
        }

        sb.Append("-----END PUBLIC KEY-----");
        return sb.ToString();
    }

    /// <summary>
    /// Parses SPKI PEM into a BouncyCastle public key. Use: Low (EncryptChallenge). Scope: enroll algorithm.
    /// </summary>
    private static AsymmetricKeyParameter ParsePublicKeyPem(string publicKeyPem)
    {
        using StringReader reader = new StringReader(publicKeyPem);
        object? parsed = new PemReader(reader).ReadObject();
        return parsed switch
        {
            AsymmetricKeyParameter key when !key.IsPrivate => key,
            AsymmetricCipherKeyPair pair => pair.Public,
            SubjectPublicKeyInfo spki => PublicKeyFactory.CreateKey(spki),
            _ => throw new CryptographicException("Unable to parse public key PEM."),
        };
    }
}
