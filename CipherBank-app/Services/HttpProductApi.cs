// <copyright file="HttpProductApi.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>Live CipherBank product /v1 HTTP client.</summary>
public sealed class HttpProductApi : IProductClient
{
    // --- Route constants (relative to CipherBankEndpointBase) ---
    private const string PortfolioPath = "v1/portfolio";
    private const string HistoryPath = "v1/history";
    private const string SessionPath = "v1/session";
    private const string SessionChallengePath = "v1/session/challenge";
    private const string SessionKeySharePath = "v1/session/key-share";
    private const string SessionRefreshPath = "v1/session/refresh";
    private const string WalletsPath = "v1/wallets";
    private const string QuotePath = "v1/quotes";
    private const string ConvertPath = "v1/convert";
    private const string TransfersPath = "v1/transfers";
    private const string PaymentsPath = "v1/payments";
    private const string ReceivePathPrefix = "v1/receive/";
    private const string VaultCardsPath = "v1/vault/cards";
    private const string VaultBinariesPath = "v1/vault/binaries";
    private const string PosSessionsPath = "v1/pos/sessions";
    private const string PosAuthorizePath = "v1/pos/authorize";
    private const string PosConfirmPath = "v1/pos/confirm";
    private const string PrefsPath = "v1/prefs";
    private const string AccountBootstrapPath = "v1/account/bootstrap";

    // --- Headers / media ---
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string JsonMediaType = "application/json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly IProductSessionStore _sessions;
    private readonly ISessionProofBuilder _sessionProof;

    public HttpProductApi(HttpClient http, IProductSessionStore sessions, ISessionProofBuilder sessionProof)
    {
        _http = http;
        _sessions = sessions;
        _sessionProof = sessionProof;
    }

    public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct = default)
        => GetAsync<PortfolioDto>(PortfolioPath, ct);

    public async Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct = default)
    {
        string path = $"{HistoryPath}?symbols={Uri.EscapeDataString(symbol)}&range={Uri.EscapeDataString(range)}";
        return await GetAsync<List<HistoryPointDto>>(path, ct).ConfigureAwait(false);
    }

    public async Task<SessionDto> CreateSessionAsync(CancellationToken ct = default)
    {
        // Lab stub today; ISessionProofBuilder will swap to challenge/pass without changing this call site.
        object body = await _sessionProof.BuildOpenBodyAsync(ct).ConfigureAwait(false);
        using var req = new HttpRequestMessage(HttpMethod.Post, SessionPath)
        {
            Content = JsonContent.Create(body),
        };
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var session = await resp.Content.ReadFromJsonAsync<SessionDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty session response.");
        await _sessions.SaveAsync(session).ConfigureAwait(false);
        return session;
    }

    public async Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, SessionChallengePath)
        {
            Content = JsonContent.Create(new { ACCOUNT_PUBLIC_KEY = accountPublicKeyWire }),
        };
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SessionChallengeDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty challenge response.");
    }

    public async Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, SessionKeySharePath)
        {
            Content = JsonContent.Create(request),
        };
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<KeyShareResponseDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty key-share response.");
    }

    public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct = default)
        => PostMutationAsync<CreateWalletResultDto>(WalletsPath, request, Guid.NewGuid().ToString("N"), ct);

    public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct = default)
        => GetAsync<QuoteDto>($"{QuotePath}?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(toAsset)}", ct);

    public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct = default)
        => PostMutationAsync<MoneyMoveDto>(ConvertPath, new { FROM = from, TO = toAsset, AMOUNT = amount }, idempotencyKey, ct);

    public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct = default)
        => PostMutationAsync<MoneyMoveDto>(TransfersPath, new { TO = destination, AMOUNT = amount, SPEED = speed }, idempotencyKey, ct);

    public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct = default)
        => PostMutationAsync<MoneyMoveDto>(PaymentsPath, new { AMOUNT = amount, MIX = mix }, idempotencyKey, ct);

    public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct = default)
        => GetAsync<ReceiveDto>(ReceivePathPrefix + Uri.EscapeDataString(asset), ct);

    public async Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct = default)
        => await GetAsync<List<VaultCardDto>>(VaultCardsPath, ct).ConfigureAwait(false);

    public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct = default)
        => PostMutationAsync<VaultCardDto>(VaultCardsPath, card, idempotencyKey, ct);

    public Task DeleteVaultCardAsync(string cardId, CancellationToken ct = default)
        => PostAsync($"{VaultCardsPath}/{Uri.EscapeDataString(cardId)}/delete", new { }, ct);

    public async Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct = default)
        => await GetAsync<List<VaultBinaryDto>>(VaultBinariesPath, ct).ConfigureAwait(false);

    public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct = default)
        => PostMutationAsync<PosSessionDto>(PosSessionsPath, new { }, Guid.NewGuid().ToString("N"), ct);

    public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct = default)
        => PostMutationAsync<PosSessionDto>(PosAuthorizePath, new { SESSION_ID = sessionId }, Guid.NewGuid().ToString("N"), ct);

    public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct = default)
        => PostMutationAsync<PosSessionDto>(PosConfirmPath, new { SESSION_ID = sessionId }, Guid.NewGuid().ToString("N"), ct);

    public async Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetAsync<PrefsWireDto>(PrefsPath, ct).ConfigureAwait(false);
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

    public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct = default)
        => PutAsync(PrefsPath, prefs, ct);

    public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct = default)
        => GetAsync<AccountBootstrapDto>(AccountBootstrapPath, ct);

    private async Task PutAsync(string path, object body, CancellationToken ct)
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using var resp = await SendWithOptionalRefreshAsync(HttpMethod.Put, path, bodyUtf8, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var resp = await SendWithOptionalRefreshAsync(HttpMethod.Get, path, bodyUtf8: null, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Empty response for {path}");
    }

    private async Task<T> PostMutationAsync<T>(string path, object body, string idempotencyKey, CancellationToken ct)
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using var resp = await SendWithOptionalRefreshAsync(HttpMethod.Post, path, bodyUtf8, idempotencyKey, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Empty response for {path}");
    }

    private async Task PostAsync(string path, object body, CancellationToken ct)
    {
        byte[] bodyUtf8 = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);
        using var resp = await SendWithOptionalRefreshAsync(HttpMethod.Post, path, bodyUtf8, idempotencyKey: null, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendWithOptionalRefreshAsync(
        HttpMethod method,
        string path,
        byte[]? bodyUtf8,
        string? idempotencyKey,
        CancellationToken ct)
    {
        var resp = await SendOnceAsync(method, path, bodyUtf8, idempotencyKey, ct).ConfigureAwait(false);
        if (resp.StatusCode != HttpStatusCode.Unauthorized)
        {
            return resp;
        }

        resp.Dispose();
        await RefreshSessionAsync(ct).ConfigureAwait(false);
        return await SendOnceAsync(method, path, bodyUtf8, idempotencyKey, ct).ConfigureAwait(false);
    }

    private async Task RefreshSessionAsync(CancellationToken ct)
    {
        var stored = await _sessions.GetAsync().ConfigureAwait(false)
            ?? throw new UnauthorizedAccessException("Product session missing.");

        using var refreshReq = new HttpRequestMessage(HttpMethod.Post, SessionRefreshPath)
        {
            Content = JsonContent.Create(new { REFRESH_TOKEN = stored.Refresh }),
        };
        using var refreshResp = await _http.SendAsync(refreshReq, ct).ConfigureAwait(false);
        refreshResp.EnsureSuccessStatusCode();
        var session = await refreshResp.Content.ReadFromJsonAsync<SessionDto>(JsonOptions, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedAccessException("Session refresh failed.");
        await _sessions.SaveAsync(session).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        byte[]? bodyUtf8,
        string? idempotencyKey,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path);
        if (bodyUtf8 is not null)
        {
            req.Content = new ByteArrayContent(bodyUtf8);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);
        }

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            req.Headers.TryAddWithoutValidation(IdempotencyHeader, idempotencyKey);
        }

        // Bearer is injected by AuthHeaderHandler from IProductSessionStore.
        return await _http.SendAsync(req, ct).ConfigureAwait(false);
    }
}
