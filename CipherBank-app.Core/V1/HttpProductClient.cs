// <copyright file="HttpProductClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>HTTP IProductClient facade for the product /v1 routes.</summary>
public sealed class HttpProductClient(
    HttpClient http,
    IProductSessionStore sessions,
    ISessionProofBuilder sessionProof,
    ProductSessionAuthority? authority) : IProductClient
{
    private const string PortfolioPath = "v1/portfolio";
    private const string HistoryPath = "v1/history";
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

    private readonly HttpProductTransport _transport = new(http, sessions, sessionProof, authority);

    public HttpProductClient(
        HttpClient http,
        IProductSessionStore sessions,
        ISessionProofBuilder sessionProof)
        : this(http, sessions, sessionProof, null)
    {
    }

    public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct)
        => _transport.GetAsync<PortfolioDto>(PortfolioPath, ct);

    public async Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct)
    {
        string path = $"{HistoryPath}?symbols={HttpProductTransport.EscapePathComponent(symbol)}&range={HttpProductTransport.EscapePathComponent(range)}";
        return await _transport.GetAsync<List<HistoryPointDto>>(path, ct).ConfigureAwait(false);
    }

    public Task<SessionDto> CreateSessionAsync(CancellationToken ct)
        => _transport.CreateSessionAsync(ct);

    public Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
        => _transport.CreateSessionChallengeAsync(accountPublicKeyWire, ct);

    public Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
        => _transport.EstablishKeyShareAsync(request, ct);

    public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct)
        => _transport.PostMutationAsync<CreateWalletResultDto>(
            WalletsPath,
            request,
            HttpProductTransport.CreateIdempotencyKey(),
            ct);

    public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct)
        => _transport.GetAsync<QuoteDto>(
            $"{QuotePath}?from={HttpProductTransport.EscapePathComponent(from)}&to={HttpProductTransport.EscapePathComponent(toAsset)}",
            ct);

    public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct)
        => _transport.PostMutationAsync<MoneyMoveDto>(ConvertPath, new { FROM = from, TO = toAsset, AMOUNT = amount }, idempotencyKey, ct);

    public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct)
        => _transport.PostMutationAsync<MoneyMoveDto>(TransfersPath, new { TO = destination, AMOUNT = amount, SPEED = speed }, idempotencyKey, ct);

    public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct)
        => _transport.PostMutationAsync<MoneyMoveDto>(PaymentsPath, new { AMOUNT = amount, MIX = mix }, idempotencyKey, ct);

    public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct)
        => _transport.GetAsync<ReceiveDto>(ReceivePathPrefix + HttpProductTransport.EscapePathComponent(asset), ct);

    public async Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct)
        => await _transport.GetAsync<List<VaultCardDto>>(VaultCardsPath, ct).ConfigureAwait(false);

    public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct)
        => _transport.PostMutationAsync<VaultCardDto>(VaultCardsPath, card, idempotencyKey, ct);

    public Task DeleteVaultCardAsync(string cardId, CancellationToken ct)
        => _transport.PostAsync($"{VaultCardsPath}/{HttpProductTransport.EscapePathComponent(cardId)}/delete", new { }, ct);

    public async Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct)
        => await _transport.GetAsync<List<VaultBinaryDto>>(VaultBinariesPath, ct).ConfigureAwait(false);

    public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct)
        => _transport.PostMutationAsync<PosSessionDto>(PosSessionsPath, new { }, HttpProductTransport.CreateIdempotencyKey(), ct);

    public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct)
        => _transport.PostMutationAsync<PosSessionDto>(
            PosAuthorizePath,
            new { SESSION_ID = sessionId },
            HttpProductTransport.CreateIdempotencyKey(),
            ct);

    public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct)
        => _transport.PostMutationAsync<PosSessionDto>(
            PosConfirmPath,
            new { SESSION_ID = sessionId },
            HttpProductTransport.CreateIdempotencyKey(),
            ct);

    public async Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct)
    {
        try
        {
            return await _transport.GetAsync<PrefsWireDto>(PrefsPath, ct).ConfigureAwait(false);
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

    public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct)
        => _transport.PutAsync(PrefsPath, prefs, ct);

    public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct)
        => _transport.GetAsync<AccountBootstrapDto>(AccountBootstrapPath, ct);

    internal Task<SessionDto> CreateSessionAsync(ProductSessionOperation operation, CancellationToken ct)
        => _transport.CreateSessionAsync(operation, ct);
}
