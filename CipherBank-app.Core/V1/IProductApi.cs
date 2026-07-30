// <copyright file="IProductApi.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>
/// CipherBank product /v1 surface. Every operation takes an explicit <see cref="CancellationToken"/>;
/// the paired zero-token defaults exist so UI call sites that have no ambient token stay source
/// compatible without reintroducing optional parameters.
/// </summary>
public interface IProductApi
{
    Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct);

    /// <summary>Portfolio snapshot for callers with no ambient token. Use: High (Home load). Scope: IProductApi consumers.</summary>
    Task<PortfolioDto> GetPortfolioAsync() => GetPortfolioAsync(CancellationToken.None);

    Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct);

    /// <summary>Sparkline history for callers with no ambient token. Use: High (Home chart). Scope: IProductApi consumers.</summary>
    Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range)
        => GetHistoryAsync(symbol, range, CancellationToken.None);

    Task<SessionDto> CreateSessionAsync(CancellationToken ct);

    /// <summary>Opens a session for callers with no ambient token. Use: Medium (unlock). Scope: IProductApi consumers.</summary>
    Task<SessionDto> CreateSessionAsync() => CreateSessionAsync(CancellationToken.None);

    Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct);

    /// <summary>Issues a session challenge for callers with no ambient token. Use: Medium (unlock). Scope: IProductApi consumers.</summary>
    Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire)
        => CreateSessionChallengeAsync(accountPublicKeyWire, CancellationToken.None);

    Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct);

    /// <summary>Establishes a key share for callers with no ambient token. Use: Low (unlock). Scope: IProductApi consumers.</summary>
    Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request)
        => EstablishKeyShareAsync(request, CancellationToken.None);

    Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct);

    /// <summary>Creates a wallet for callers with no ambient token. Use: Low (add wallet). Scope: IProductApi consumers.</summary>
    Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request)
        => CreateWalletAsync(request, CancellationToken.None);

    Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct);

    /// <summary>Indicative quote for callers with no ambient token. Use: High (Convert). Scope: IProductApi consumers.</summary>
    Task<QuoteDto> GetQuoteAsync(string from, string toAsset) => GetQuoteAsync(from, toAsset, CancellationToken.None);

    Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct);

    /// <summary>Convert for callers with no ambient token. Use: Medium (Convert submit). Scope: IProductApi consumers.</summary>
    Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey)
        => ConvertAsync(from, toAsset, amount, idempotencyKey, CancellationToken.None);

    Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct);

    /// <summary>Transfer for callers with no ambient token. Use: Medium (Send submit). Scope: IProductApi consumers.</summary>
    Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey)
        => TransferAsync(destination, amount, speed, idempotencyKey, CancellationToken.None);

    Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct);

    /// <summary>Pay for callers with no ambient token. Use: Medium (Pay submit). Scope: IProductApi consumers.</summary>
    Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey)
        => PayAsync(amount, mix, idempotencyKey, CancellationToken.None);

    Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct);

    /// <summary>Receive address for callers with no ambient token. Use: Medium (Receive). Scope: IProductApi consumers.</summary>
    Task<ReceiveDto> GetReceiveAsync(string asset) => GetReceiveAsync(asset, CancellationToken.None);

    Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct);

    /// <summary>Vault cards for callers with no ambient token. Use: Medium (Profile). Scope: IProductApi consumers.</summary>
    Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync() => GetVaultCardsAsync(CancellationToken.None);

    Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct);

    /// <summary>Adds a vault card for callers with no ambient token. Use: Low (Profile). Scope: IProductApi consumers.</summary>
    Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey)
        => AddVaultCardAsync(card, idempotencyKey, CancellationToken.None);

    Task DeleteVaultCardAsync(string cardId, CancellationToken ct);

    /// <summary>Deletes a vault card for callers with no ambient token. Use: Low (Profile). Scope: IProductApi consumers.</summary>
    Task DeleteVaultCardAsync(string cardId) => DeleteVaultCardAsync(cardId, CancellationToken.None);

    Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct);

    /// <summary>Vault binaries for callers with no ambient token. Use: Medium (Profile). Scope: IProductApi consumers.</summary>
    Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync() => GetVaultBinariesAsync(CancellationToken.None);

    Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct);

    /// <summary>Opens a POS session for callers with no ambient token. Use: Low (PosLab). Scope: IProductApi consumers.</summary>
    Task<PosSessionDto> CreatePosSessionAsync() => CreatePosSessionAsync(CancellationToken.None);

    Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct);

    /// <summary>Authorizes a POS session for callers with no ambient token. Use: Low (PosLab). Scope: IProductApi consumers.</summary>
    Task<PosSessionDto> AuthorizePosAsync(string sessionId) => AuthorizePosAsync(sessionId, CancellationToken.None);

    Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct);

    /// <summary>Confirms a POS session for callers with no ambient token. Use: Low (PosLab). Scope: IProductApi consumers.</summary>
    Task<PosSessionDto> ConfirmPosAsync(string sessionId) => ConfirmPosAsync(sessionId, CancellationToken.None);

    Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct);

    /// <summary>Reads prefs for callers with no ambient token. Use: Medium (sync). Scope: IProductApi consumers.</summary>
    Task<PrefsWireDto?> GetPrefsAsync() => GetPrefsAsync(CancellationToken.None);

    Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct);

    /// <summary>Writes prefs for callers with no ambient token. Use: Medium (sync). Scope: IProductApi consumers.</summary>
    Task PutPrefsAsync(PrefsWireDto prefs) => PutPrefsAsync(prefs, CancellationToken.None);

    Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct);

    /// <summary>Account bootstrap for callers with no ambient token. Use: Low (startup). Scope: IProductApi consumers.</summary>
    Task<AccountBootstrapDto> GetAccountBootstrapAsync() => GetAccountBootstrapAsync(CancellationToken.None);
}
