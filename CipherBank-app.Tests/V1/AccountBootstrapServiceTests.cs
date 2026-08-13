// <copyright file="AccountBootstrapServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class AccountBootstrapServiceTests
{
    [Fact]
    public async Task ApplyAsync_SkipsRecipientWhenAccountLast4Missing()
    {
        MemPrefs prefs = new MemPrefs();
        MemRecipients recipients = new MemRecipients();
        BootstrapApi api = new BootstrapApi(new AccountBootstrapDto
        {
            Prefs = new PrefsWireDto { DefaultSendSpeed = "instant", CoraEnabled = true },
            Recipients =
            {
                new BootstrapRecipientDto
                {
                    Id = "no-last4",
                    DisplayName = "No Mask",
                    AccountHolderName = "No Mask",
                    BankName = "Chase",
                    RoutingNumber = "021000021",
                    AccountLast4 = null,
                    AccountType = "checking",
                },
                new BootstrapRecipientDto
                {
                    Id = "with-last4",
                    DisplayName = "Has Mask",
                    AccountHolderName = "Has Mask",
                    BankName = "Chase",
                    RoutingNumber = "021000021",
                    AccountLast4 = "4021",
                    AccountType = "checking",
                },
            },
        });

        AccountBootstrapService svc = new AccountBootstrapService(api, prefs, recipients);
        await svc.ApplyAsync(CancellationToken.None);

        recipients.Rows.Should().ContainSingle(r => r.Id == "with-last4");
        recipients.Rows.Should().NotContain(r => r.Id == "no-last4");
        recipients.Rows.Single().AccountMask.Should().Be(AchRecipientValidation.MaskAccount("****4021"));
    }

    private sealed class BootstrapApi : IProductClient
    {
        private readonly AccountBootstrapDto _bootstrap;
        private readonly InMemoryProductClient _inner = new();

        public BootstrapApi(AccountBootstrapDto bootstrap)
        {
            _bootstrap = bootstrap;
        }

        public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct)
            => Task.FromResult(_bootstrap);

        public Task<SessionDto> CreateSessionAsync(CancellationToken ct) => _inner.CreateSessionAsync(ct);

        public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct) => _inner.GetPortfolioAsync(ct);

        public Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct)
            => _inner.GetHistoryAsync(symbol, range, ct);

        public Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
            => _inner.CreateSessionChallengeAsync(accountPublicKeyWire, ct);

        public Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
            => _inner.EstablishKeyShareAsync(request, ct);

        public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct)
            => _inner.CreateWalletAsync(request, ct);

        public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct)
            => _inner.GetQuoteAsync(from, toAsset, ct);

        public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct)
            => _inner.ConvertAsync(from, toAsset, amount, idempotencyKey, ct);

        public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct)
            => _inner.TransferAsync(destination, amount, speed, idempotencyKey, ct);

        public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct)
            => _inner.PayAsync(amount, mix, idempotencyKey, ct);

        public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct)
            => _inner.GetReceiveAsync(asset, ct);

        public Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct)
            => _inner.GetVaultCardsAsync(ct);

        public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct)
            => _inner.AddVaultCardAsync(card, idempotencyKey, ct);

        public Task DeleteVaultCardAsync(string cardId, CancellationToken ct)
            => _inner.DeleteVaultCardAsync(cardId, ct);

        public Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct)
            => _inner.GetVaultBinariesAsync(ct);

        public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct)
            => _inner.CreatePosSessionAsync(ct);

        public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct)
            => _inner.AuthorizePosAsync(sessionId, ct);

        public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct)
            => _inner.ConfirmPosAsync(sessionId, ct);

        public Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct)
            => _inner.GetPrefsAsync(ct);

        public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct)
            => _inner.PutPrefsAsync(prefs, ct);
    }

    private sealed class MemPrefs : IPrefsStore
    {
        public UserPrefs Current { get; set; } = new();

        public Task<UserPrefs> LoadAsync() => Task.FromResult(Current);

        public Task SaveAsync(UserPrefs prefs)
        {
            Current = prefs;
            return Task.CompletedTask;
        }
    }

    private sealed class MemRecipients : IRecipientRepository
    {
        public List<AchRecipientRow> Rows { get; } = [];

        public Task EnsureSchemaAsync() => Task.CompletedTask;

        public Task<IReadOnlyList<AchRecipientRow>> ListAsync()
            => Task.FromResult<IReadOnlyList<AchRecipientRow>>(Rows);

        public Task UpsertAsync(AchRecipientRow row)
        {
            Rows.RemoveAll(r => r.Id == row.Id);
            Rows.Add(row);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            Rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task SeedDefaultsIfEmptyAsync() => Task.CompletedTask;
    }
}
