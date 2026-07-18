// <copyright file="IProductApi.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>CipherBank product /v1 surface.</summary>
public interface IProductApi
{
    Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct = default);

    Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct = default);

    Task<SessionDto> CreateSessionAsync(CancellationToken ct = default);

    Task<QuoteDto> GetQuoteAsync(string from, string to, CancellationToken ct = default);

    Task<MoneyMoveDto> ConvertAsync(string from, string to, string amount, string idempotencyKey, CancellationToken ct = default);

    Task<MoneyMoveDto> TransferAsync(string to, string amount, string speed, string idempotencyKey, CancellationToken ct = default);

    Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct = default);

    Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct = default);

    Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct = default);

    Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct = default);

    Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct = default);

    Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct = default);
}
