// <copyright file="HttpProductTransport.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>HTTP request, serialization, and session refresh mechanics. Use: HttpProductClient. Scope: V1 product requests.</summary>
internal sealed class HttpProductTransport
{
    private const string SessionPath = "v1/session";
    private const string SessionChallengePath = "v1/session/challenge";
    private const string SessionKeySharePath = "v1/session/key-share";
    private const string SessionRefreshPath = "v1/session/refresh";
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string JsonMediaType = "application/json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ProductSessionAuthority _sessions;
    private readonly ISessionProofBuilder _sessionProof;

    internal HttpProductTransport(
        HttpClient http,
        ProductSessionAuthority sessions,
        ISessionProofBuilder sessionProof)
    {
        _http = http;
        _sessions = sessions;
        _sessionProof = sessionProof;
    }

    internal static string EscapePathComponent(string value)
        => Uri.EscapeDataString(value);

    internal static string CreateIdempotencyKey()
        => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

    internal async Task<SessionDto> CreateSessionAsync(CancellationToken ct)
        => await CreateSessionAsync(_sessions.Begin(), ct).ConfigureAwait(false);

    internal async Task<SessionDto> CreateSessionAsync(ProductSessionOperation operation, CancellationToken ct)
    {
        _sessions.Check(operation, ct);

        // The proof builder owns the custody proof protocol behind this call site.
        object body = await _sessionProof.BuildOpenBodyAsync(ct).ConfigureAwait(false);
        using HttpRequestMessage req = new(HttpMethod.Post, new Uri(SessionPath, UriKind.Relative))
        {
            Content = JsonContent.Create(body),
        };
        _sessions.Check(operation, ct);
        ProductSessionDispatch.SetOperation(req, operation);
        using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        _sessions.Check(operation, ct);
        resp.EnsureSuccessStatusCode();
        SessionDto? session = await resp.Content.ReadFromJsonAsync<SessionDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty session response.");
        await _sessions.PublishAsync(operation, session, ct).ConfigureAwait(false);
        _sessions.Check(operation, ct);
        return session;
    }

    internal async Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
    {
        using HttpRequestMessage req = new(HttpMethod.Post, new Uri(SessionChallengePath, UriKind.Relative))
        {
            Content = JsonContent.Create(new { ACCOUNT_PUBLIC_KEY = accountPublicKeyWire }),
        };
        using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SessionChallengeDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty challenge response.");
    }

    internal async Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
    {
        using HttpRequestMessage req = new(HttpMethod.Post, new Uri(SessionKeySharePath, UriKind.Relative))
        {
            Content = JsonContent.Create(request),
        };
        using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<KeyShareResponseDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty key-share response.");
    }

    internal async Task PutAsync(string path, object body, CancellationToken ct)
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using HttpResponseMessage resp = await SendWithOptionalRefreshAsync(HttpMethod.Put, path, bodyUtf8, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    internal async Task<T> GetAsync<T>(string path, CancellationToken ct) // NOSONAR S4018 — response-only type param
    {
        using HttpResponseMessage resp = await SendWithOptionalRefreshAsync(HttpMethod.Get, path, bodyUtf8: null, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Empty response for {path}");
    }

    internal async Task<PrefsWireDto?> GetPrefsOrNullAsync(string path, CancellationToken ct)
    {
        try
        {
            return await GetAsync<PrefsWireDto>(path, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    internal async Task<T> PostMutationAsync<T>(string path, object body, string idempotencyKey, CancellationToken ct) // NOSONAR S4018 — response-only type param
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using HttpResponseMessage resp = await SendWithOptionalRefreshAsync(HttpMethod.Post, path, bodyUtf8, idempotencyKey, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Empty response for {path}");
    }

    internal async Task PostAsync(string path, object body, CancellationToken ct)
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using HttpResponseMessage resp = await SendWithOptionalRefreshAsync(HttpMethod.Post, path, bodyUtf8, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendWithOptionalRefreshAsync(
        HttpMethod method,
        string path,
        byte[]? bodyUtf8,
        string? idempotencyKey,
        CancellationToken ct)
    {
        ProductSessionOperation operation = _sessions.Capture();
        ProductSessionSnapshot? original = await _sessions.ReadAsync(operation, ct).ConfigureAwait(false);
        HttpResponseMessage resp = await SendOnceAsync(method, path, bodyUtf8, idempotencyKey, original, operation, ct).ConfigureAwait(false);
        if (resp.StatusCode != HttpStatusCode.Unauthorized)
        {
            return resp;
        }

        resp.Dispose();
        if (original is null)
        {
            throw new UnauthorizedAccessException("Product session missing.");
        }

        ProductSessionSnapshot refreshed = await _sessions.RefreshAsync(
            original,
            token => RefreshSessionAsync(original, token),
            ct).ConfigureAwait(false);
        _sessions.Check(operation, ct);
        return await SendOnceAsync(method, path, bodyUtf8, idempotencyKey, refreshed, operation, ct).ConfigureAwait(false);
    }

    private async Task<SessionDto> RefreshSessionAsync(ProductSessionSnapshot original, CancellationToken ct)
    {
        _sessions.Check(original.Operation, ct);
        using HttpRequestMessage refreshReq = new(HttpMethod.Post, new Uri(SessionRefreshPath, UriKind.Relative))
        {
            Content = JsonContent.Create(new { REFRESH_TOKEN = original.Refresh }),
        };
        ProductSessionDispatch.SetOperation(refreshReq, original.Operation);
        ProductSessionDispatch.Set(refreshReq, original);
        using HttpResponseMessage refreshResp = await _http.SendAsync(refreshReq, ct).ConfigureAwait(false);
        refreshResp.EnsureSuccessStatusCode();
        return await refreshResp.Content.ReadFromJsonAsync<SessionDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedAccessException("Session refresh failed.");
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        byte[]? bodyUtf8,
        string? idempotencyKey,
        ProductSessionSnapshot? snapshot,
        ProductSessionOperation operation,
        CancellationToken ct)
    {
        _sessions.Check(operation, ct);
        using HttpRequestMessage req = new(method, new Uri(path, UriKind.Relative));
        if (bodyUtf8 is not null)
        {
            req.Content = new ByteArrayContent(bodyUtf8);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);
        }

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            req.Headers.TryAddWithoutValidation(IdempotencyHeader, idempotencyKey);
        }

        ProductSessionDispatch.SetOperation(req, operation);
        if (snapshot is not null)
        {
            ProductSessionDispatch.Set(req, snapshot);
        }

        // Admission cannot retract a request already transmitted to the transport.
        _sessions.Check(operation, ct);
        return await _http.SendAsync(req, ct).ConfigureAwait(false);
    }
}
