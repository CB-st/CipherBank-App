// <copyright file="NullNfcPresentmentService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Pos;

/// <summary>No-op NFC for non-Android / unsupported devices.</summary>
public sealed class NullNfcPresentmentService : INfcPresentmentService
{
    public bool IsSupported => false;

    public string? LastError { get; private set; } = "NFC presentment is only available on Android devices with NFC.";

    public Task<bool> PresentAsync(NfcPresentmentPayload payload, CancellationToken ct)
        => PresentCore(payload, ct);

    public Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout, CancellationToken ct)
    {
        _ = timeout;
        return PresentCore(payload, ct);
    }

    private Task<bool> PresentCore(NfcPresentmentPayload payload, CancellationToken ct)
    {
        _ = payload;
        _ = ct;
        LastError = "NFC unavailable on this platform — use Simulate exchange.";
        return Task.FromResult(false);
    }
}
