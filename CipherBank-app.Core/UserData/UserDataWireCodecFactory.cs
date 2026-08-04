// <copyright file="UserDataWireCodecFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Selects a wire codec from endpoint payload mode.</summary>
public static class UserDataWireCodecFactory
{
    /// <summary>
    /// Resolves PlainJson or throws for MasterKeyEncrypted until Encrypter keys are wired.
    /// Use: Medium (transport DIY). Scope: userdata Core.
    /// </summary>
    public static IUserDataWireCodec Create(UserDataPayloadMode mode)
        => mode switch
        {
            UserDataPayloadMode.PlainJson => new PlainJsonUserDataWireCodec(),
            UserDataPayloadMode.MasterKeyEncrypted => throw new NotSupportedException(
                "MasterKeyEncrypted userdata PAYLOAD requires CB_MASTER_KEY Encrypter wiring; use PlainJson for loopback/self-test."),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
}
