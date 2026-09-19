// <copyright file="UserDataCryptoSuites.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Built-in userdata crypto suite factories.</summary>
public static class UserDataCryptoSuites
{
    /// <summary>
    /// Builds the shipped v1 RSA-OAEP + AES-GCM suite.
    /// Use: Low (catalog / DI). Scope: userdata crypto.
    /// </summary>
    public static UserDataCryptoSuite CreateRsaAesGcmV1()
    {
        IUserDataSymmetricCipher symmetric = new AesGcmUserDataSymmetricCipher();
        return new UserDataCryptoSuite(
            UserDataConstants.SuiteRsaAesGcmV1,
            new RsaOaepSha256UserDataEnrollAlgorithm(),
            new AesGcmUserDataBlockCipher(symmetric),
            symmetric);
    }
}
