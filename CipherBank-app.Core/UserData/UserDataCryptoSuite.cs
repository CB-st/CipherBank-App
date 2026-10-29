// <copyright file="UserDataCryptoSuite.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Named composition of enroll + block + internal symmetric slots (ChallengePass-suite pattern).
/// PQ swap later: new enroll implementation + new suite id; re-enroll required.
/// </summary>
public sealed class UserDataCryptoSuite
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataCryptoSuite"/> class.
    /// Binds algorithm slots for a stable suite id. Use: Low (catalog). Scope: userdata crypto.
    /// </summary>
    public UserDataCryptoSuite(
        string suiteId,
        IUserDataEnrollAlgorithm enroll,
        IUserDataBlockCipher blocks,
        IUserDataSymmetricCipher symmetric)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        ArgumentNullException.ThrowIfNull(enroll);
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(symmetric);

        SuiteId = suiteId;
        Enroll = enroll;
        Blocks = blocks;
        Symmetric = symmetric;
    }

    public string SuiteId { get; }

    public IUserDataEnrollAlgorithm Enroll { get; }

    public IUserDataBlockCipher Blocks { get; }

    /// <summary>Raw AEAD for Core-internal wrapping (not wire enroll).</summary>
    public IUserDataSymmetricCipher Symmetric { get; }
}
