// <copyright file="UserDataPayloadMode.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// How PAYLOAD is encoded on CIPHERBANK_INTERNAL frames.
/// PlainJson is for loopback / self-test; MasterKeyEncrypted matches src Encrypter once keys are supplied.
/// </summary>
public enum UserDataPayloadMode
{
    /// <summary>PAYLOAD is a JSON object (no CB_MASTER_KEY). Loopback + unit/E2E self-server.</summary>
    PlainJson = 0,

    /// <summary>PAYLOAD is Encrypter base64(ChaCha(idx=TIME_STAMP)). Production src compatibility.</summary>
    MasterKeyEncrypted = 1,
}
