// <copyright file="QrCodeGenerator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using QRCoder;

namespace CipherBank_app.Wallets;

/// <summary>QR matrix / PNG generation for receive URIs.</summary>
public static class QrCodeGenerator
{
    private const int DefaultPixelsPerModule = 8;

    public static byte[] ToPngBytes(string payload)
        => ToPngBytes(payload, DefaultPixelsPerModule);

    public static byte[] ToPngBytes(string payload, int pixelsPerModule)
    {
        using var gen = new QRCodeGenerator();
        using QRCodeData data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
