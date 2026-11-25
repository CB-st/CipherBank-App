// <copyright file="AccountBootstrapServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class AccountBootstrapServiceTests
{
    [Fact]
    public async Task ApplyAsync_SkipsRecipientWhenAccountLast4Missing()
    {
        MemPrefs prefs = new();
        MemRecipients recipients = new();
        BootstrapApi api = new(new AccountBootstrapDto
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

        AccountBootstrapService svc = new(api, prefs, recipients);
        await svc.ApplyAsync(CancellationToken.None);

        recipients.Rows.Should().ContainSingle(r => r.Id == "with-last4");
        recipients.Rows.Should().NotContain(r => r.Id == "no-last4");
        recipients.Rows.Single().AccountMask.Should().Be(AchRecipientValidation.MaskAccount("****4021"));
    }

    [Fact]
    public async Task ApplyAsync_PersistsMergedPrefsAndAllValidRecipients()
    {
        MemPrefs prefs = new();
        MemRecipients recipients = new();
        BootstrapApi api = new(new AccountBootstrapDto
        {
            Prefs = new PrefsWireDto { DefaultSendSpeed = "ach", CoraEnabled = true },
            Recipients =
            {
                CreateRecipient("first", "First Payee", "1234"),
                CreateRecipient("second", "Second Payee", "9876"),
            },
        });

        AccountBootstrapService svc = new(api, prefs, recipients);
        await svc.ApplyAsync(CancellationToken.None);

        prefs.Current.DefaultSendSpeed.Should().Be("ach");
        prefs.Current.CoraEnabled.Should().BeTrue();
        recipients.Rows.Should().HaveCount(2);
        recipients.Rows.Should().ContainSingle(row => row.Id == "first"
            && row.Name == "First Payee"
            && row.AccountMask == AchRecipientValidation.MaskAccount("****1234")
            && row.RoutingMask == AchRecipientValidation.MaskRouting("021000021"));
        recipients.Rows.Should().ContainSingle(row => row.Id == "second"
            && row.Name == "Second Payee"
            && row.AccountMask == AchRecipientValidation.MaskAccount("****9876")
            && row.RoutingMask == AchRecipientValidation.MaskRouting("021000021"));
    }

    [Fact]
    public async Task ApplyAsync_RestoresPrefsAndRecipientsWhenLaterWriteFails()
    {
        MemPrefs prefs = new();
        prefs.Current.Appearance = "dark";
        prefs.Current.CoraEnabled = false;
        MemRecipients recipients = new() { ThrowAfterUpsertNumber = 2 };
        AchRecipientRow existing = new(
            "existing",
            "Existing Payee",
            "Existing Payee",
            "Bank",
            null,
            null,
            "checking",
            null,
            "••••1111",
            "••••0021",
            DateTimeOffset.UnixEpoch);
        recipients.Rows.Add(existing);
        BootstrapApi api = new(new AccountBootstrapDto
        {
            Prefs = new PrefsWireDto { DefaultSendSpeed = "ach", CoraEnabled = true },
            Recipients =
            {
                CreateRecipient("first", "First Payee", "1234"),
                CreateRecipient("second", "Second Payee", "9876"),
            },
        });
        AccountBootstrapService svc = new(api, prefs, recipients);
        Func<Task> apply = () => svc.ApplyAsync(CancellationToken.None);
        await apply.Should().ThrowAsync<InvalidOperationException>();

        prefs.Current.DefaultSendSpeed.Should().Be("instant");
        prefs.Current.CoraEnabled.Should().BeFalse();
        prefs.Current.Appearance.Should().Be("dark");
        recipients.Rows.Should().BeEquivalentTo([existing]);
    }

    private static BootstrapRecipientDto CreateRecipient(string id, string name, string last4)
        => new()
        {
            Id = id,
            DisplayName = name,
            AccountHolderName = name,
            BankName = "Chase",
            RoutingNumber = "021000021",
            AccountLast4 = last4,
            AccountType = "checking",
        };

    private sealed class BootstrapApi(AccountBootstrapDto bootstrap) : IProductClient
    {
        private readonly AccountBootstrapDto _bootstrap = bootstrap;
        private readonly InMemoryProductClient _inner = new();

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
        public UserPrefs Current { get; set; } = new(TestPreferenceDefaults.Value);

        public Task<UserPrefs> LoadAsync(CancellationToken ct = default) => Task.FromResult(Current);

        public Task SaveAsync(UserPrefs prefs, CancellationToken ct = default)
        {
            Current = prefs;
            return Task.CompletedTask;
        }
    }

    private sealed class MemRecipients : IRecipientRepository
    {
        private int _upsertCount;

        public List<AchRecipientRow> Rows { get; } = [];

        public int? ThrowAfterUpsertNumber { get; init; }

        public Task<IReadOnlyList<AchRecipientRow>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AchRecipientRow>>(Rows);

        public Task UpsertAsync(AchRecipientRow row, CancellationToken ct = default)
        {
            _upsertCount++;
            Rows.RemoveAll(r => r.Id == row.Id);
            Rows.Add(row);
            if (_upsertCount == ThrowAfterUpsertNumber)
            {
                throw new InvalidOperationException("Injected recipient write failure.");
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }
    }
}
