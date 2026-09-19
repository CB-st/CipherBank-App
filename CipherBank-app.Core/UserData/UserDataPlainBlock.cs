// <copyright file="UserDataPlainBlock.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Unsealed block input for pack construction.</summary>
public sealed class UserDataPlainBlock
{
    public UserDataPlainBlock(string id, string type, string plaintextUtf8)
        : this(id, type, plaintextUtf8, seq: 0)
    {
    }

    public UserDataPlainBlock(string id, string type, string plaintextUtf8, uint seq)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(plaintextUtf8);

        Id = id;
        Type = type;
        PlaintextUtf8 = plaintextUtf8;
        Seq = seq;
    }

    public string Id { get; }

    public string Type { get; }

    public string PlaintextUtf8 { get; }

    public uint Seq { get; }
}
