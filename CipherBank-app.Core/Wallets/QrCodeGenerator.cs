// <copyright file="QrCodeGenerator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using QRCoder;

namespace CipherBank_app.Wallets;

/// <summary>
/// QR PNG generation for receive URIs.
/// A generator is created per call: <see cref="QRCodeGenerator"/> is disposable and is not
/// shared across threads. PNG bytes are the portable payload; a bitmap would pull a UI stack
/// into platform-neutral Core.
/// </summary>
public static class QrCodeGenerator
{
    private const int DefaultPixelsPerModule = 8;

    /// <summary>Encodes <paramref name="payload"/> as a PNG QR.</summary>
    public static QrPng ToPng(string payload)
        => ToPng(payload, DefaultPixelsPerModule);

    /// <summary>Encodes <paramref name="payload"/> as a PNG QR at <paramref name="pixelsPerModule"/>.</summary>
    public static QrPng ToPng(string payload, int pixelsPerModule)
    {
        using QRCodeGenerator generator = new QRCodeGenerator();
        using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        PngByteQRCode png = new PngByteQRCode(data);
        return new QrPng(png.GetGraphic(pixelsPerModule));
    }
}
