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
            JsonElement root = doc.RootElement;
            return new NfcPresentmentPayload
            {
                V = root.TryGetProperty("v", out JsonElement v) ? v.GetInt32() : 1,
                SessionId = root.TryGetProperty("sessionId", out JsonElement s) ? s.GetString() ?? string.Empty : string.Empty,
                TokenRef = root.TryGetProperty("tokenRef", out JsonElement t) ? t.GetString() ?? string.Empty : string.Empty,
                MerchantId = root.TryGetProperty("merchantId", out JsonElement m) ? m.GetString() : null,
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

    Task<bool> PresentAsync(NfcPresentmentPayload payload, CancellationToken ct);

    Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout, CancellationToken ct);
}

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

/// <summary>Simulated EMV exchange stages for PosLab UI.</summary>
public static class EmvExchangeSimulator
{
    private const int StageDelayMs = 400;

    public static IReadOnlyList<string> Stages { get; } = new[]
    {
        "SELECT PPSE",
        "SELECT AID",
        "GET PROCESSING OPTIONS",
        "GENERATE AC",
        "OUTCOME: APPROVED",
    };

    public static async IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        foreach (string stage in Stages)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(StageDelayMs, ct).ConfigureAwait(false);
            yield return stage;
        }
    }
}
