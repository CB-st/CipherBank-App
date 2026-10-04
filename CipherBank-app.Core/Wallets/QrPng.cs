// <copyright file="QrPng.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>PNG bytes for a receive QR. Core stays free of bitmap and MAUI image types.</summary>
public readonly record struct QrPng(ReadOnlyMemory<byte> Bytes)
{
    /// <summary>Media type for the payload in <see cref="Bytes"/>.</summary>
    public const string MediaType = "image/png";
}
