// <copyright file="WireEncoding.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>URL-safe base64 without padding for pubkey / ciphertext wire fields.</summary>
public static class WireEncoding
{
    public static string ToWire(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static byte[] FromWire(string wire)
    {
        string padded = wire.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }
}
