// <copyright file="WireEncoding.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>URL-safe base64 without padding for pubkey / ciphertext wire fields.</summary>
public static class WireEncoding
{
    private const int Base64BlockSize = 4;
    private const int Base64RemainderNeedsTwoPadChars = 2;
    private const int Base64RemainderNeedsOnePadChar = 3;

    public static string ToWire(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static byte[] FromWire(string wire)
    {
        string padded = wire.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % Base64BlockSize)
        {
            case Base64RemainderNeedsTwoPadChars:
                padded += "==";
                break;
            case Base64RemainderNeedsOnePadChar:
                padded += "=";
                break;
            case 0:
                break;
            default:
                throw new FormatException($"Invalid wire-encoded length: '{wire}'.");
        }

        return Convert.FromBase64String(padded);
    }
}
