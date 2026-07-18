// <copyright file="QrCodeGenerator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using QRCoder;

namespace CipherBank_app.Wallets;

/// <summary>QR matrix / PNG generation for receive URIs.</summary>
public static class QrCodeGenerator
{
    public static byte[] ToPngBytes(string payload, int pixelsPerModule = 8)
    {
        using var gen = new QRCodeGenerator();
        using QRCodeData data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
