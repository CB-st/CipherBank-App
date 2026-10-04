// <copyright file="UserDataAad.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text;

namespace CipherBank_app.UserData;

/// <summary>Builds AES-GCM additional authenticated data for userdata blocks.</summary>
public static class UserDataAad
{
    /// <summary>
    /// Builds AAD: <c>cipherbank-userdata-v1|{username_hash_hex}|{type}|{id}|{content_version}</c>.
    /// Use: High (every block seal/open). Scope: UserDataPackCodec.
    /// </summary>
    public static byte[] Build(string usernameHashHex, string type, string id, uint contentVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usernameHashHex);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        string aad =
            $"{UserDataConstants.AadPrefix}|{usernameHashHex}|{type}|{id}|{UserDataUsernameHash.FormatVersion(contentVersion)}";
        return Encoding.UTF8.GetBytes(aad);
    }
}
