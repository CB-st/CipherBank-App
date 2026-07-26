// <copyright file="IProductApi.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>CipherBank product /v1 surface.</summary>
public interface IProductApi
{
    Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct);

    Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct);

    Task<SessionDto> CreateSessionAsync(CancellationToken ct);

    Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct);

    Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct);

    Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct);

    Task<QuoteDto> GetQuoteAsync(string from, string to, CancellationToken ct);

    Task<MoneyMoveDto> ConvertAsync(string from, string to, string amount, string idempotencyKey, CancellationToken ct);

    Task<MoneyMoveDto> TransferAsync(string to, string amount, string speed, string idempotencyKey, CancellationToken ct);

    Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct);

    Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct);

    Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct);

    Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct);

    Task DeleteVaultCardAsync(string cardId, CancellationToken ct);

    Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct);

    Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct);

    Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct);

    Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct);

    Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct);

    Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct);

    Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct);
}
