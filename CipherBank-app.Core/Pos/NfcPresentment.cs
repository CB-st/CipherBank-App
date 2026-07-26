// <copyright file="NfcPresentment.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.Pos;

/// <summary>NDEF presentment payload (tokenRef only — no PAN).</summary>
public sealed class NfcPresentmentPayload
{
    public int V { get; set; } = 1;

    public string SessionId { get; set; } = string.Empty;

    public string TokenRef { get; set; } = string.Empty;

    public string? MerchantId { get; set; }

    public string ToJson()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["v"] = V,
            ["sessionId"] = SessionId,
            ["tokenRef"] = TokenRef,
            ["merchantId"] = MerchantId,
        });

    public static NfcPresentmentPayload? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new NfcPresentmentPayload
            {
                V = root.TryGetProperty("v", out var v) ? v.GetInt32() : 1,
                SessionId = root.TryGetProperty("sessionId", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                TokenRef = root.TryGetProperty("tokenRef", out var t) ? t.GetString() ?? string.Empty : string.Empty,
                MerchantId = root.TryGetProperty("merchantId", out var m) ? m.GetString() : null,
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>Platform NFC presentment (Android NDEF; others stub).</summary>
public interface INfcPresentmentService
{
    bool IsSupported { get; }

    string? LastError { get; }

    Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan? timeout = null, CancellationToken ct = default);
}

/// <summary>No-op NFC for non-Android / unsupported devices.</summary>
public sealed class NullNfcPresentmentService : INfcPresentmentService
{
    public bool IsSupported => false;

    public string? LastError { get; private set; } = "NFC presentment is only available on Android devices with NFC.";

    public Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        LastError = "NFC unavailable on this platform — use Simulate exchange.";
        return Task.FromResult(false);
    }
}

/// <summary>Simulated EMV exchange stages for PosLab UI.</summary>
public static class EmvExchangeSimulator
{
    public static IReadOnlyList<string> Stages { get; } = new[]
    {
        "SELECT PPSE",
        "SELECT AID",
        "GET PROCESSING OPTIONS",
        "GENERATE AC",
        "OUTCOME: APPROVED",
    };

    public static async IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (string stage in Stages)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(400, ct).ConfigureAwait(false);
            yield return stage;
        }
    }
}
