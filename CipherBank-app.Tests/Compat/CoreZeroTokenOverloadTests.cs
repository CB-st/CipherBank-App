// <copyright file="CoreZeroTokenOverloadTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Models;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using CipherBank_app.Services;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Compat;

/// <summary>
/// Guards the zero-token convenience overloads Core exposes for UI call sites that have no ambient
/// token. Each stub implements only the CancellationToken-required members, so these tests also prove
/// implementers stay source compatible without hand-writing the overloads.
/// </summary>
public class CoreZeroTokenOverloadTests
{
    /// <summary>
    /// Exercises every IProductApi zero-token overload and asserts each forwards CancellationToken.None.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task ProductApi_ZeroTokenOverloads_ForwardNone()
    {
        var stub = new RecordingProductApi();
        IProductApi api = stub;

        await api.GetPortfolioAsync();
        await api.GetHistoryAsync("BTC", "1d");
        await api.CreateSessionAsync();
        await api.CreateSessionChallengeAsync("wire");
        await api.EstablishKeyShareAsync(null!);
        await api.CreateWalletAsync(null!);
        await api.GetQuoteAsync("BTC", "USD");
        await api.ConvertAsync("BTC", "USD", "1", "idem");
        await api.TransferAsync("addr", "1", "fast", "idem");
        await api.PayAsync("1", new Dictionary<string, string>(), "idem");
        await api.GetReceiveAsync("BTC");
        await api.GetVaultCardsAsync();
        await api.AddVaultCardAsync(null!, "idem");
        await api.DeleteVaultCardAsync("card");
        await api.GetVaultBinariesAsync();
        await api.CreatePosSessionAsync();
        await api.AuthorizePosAsync("sess");
        await api.ConfirmPosAsync("sess");
        await api.GetPrefsAsync();
        await api.PutPrefsAsync(null!);
        await api.GetAccountBootstrapAsync();

        stub.Seen.Should().HaveCount(21).And.OnlyContain(t => t == CancellationToken.None);
    }

    /// <summary>
    /// Exercises the quote, wallet, and prefs-sync zero-token overloads.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task Services_ZeroTokenOverloads_ForwardNone()
    {
        var quotes = new RecordingQuoteService();
        IPublicQuoteService quoteApi = quotes;
        await quoteApi.TestConnectionAsync();
        await quoteApi.GetCurrenciesAsync();
        await quoteApi.GetInverseQuoteAsync("BTC", 1m, "USD");
        await quoteApi.GetQuoteAsync("BTC", 1m, "USD");

        var wallets = new RecordingWalletService();
        IWalletService walletApi = wallets;
        await walletApi.GetWalletsAsync();
        await walletApi.GetWalletAsync("id");
        await walletApi.GetWalletBalanceAsync("id");
        await walletApi.CreateWalletAsync("BTC");

        var prefs = new RecordingPrefsSync();
        IPrefsSyncService prefsApi = prefs;
        await prefsApi.PullMergeAsync();
        await prefsApi.SaveAndPushAsync(new UserPrefs());

        quotes.Seen.Should().HaveCount(4).And.OnlyContain(t => t == CancellationToken.None);
        wallets.Seen.Should().HaveCount(4).And.OnlyContain(t => t == CancellationToken.None);
        prefs.Seen.Should().HaveCount(2).And.OnlyContain(t => t == CancellationToken.None);
    }

    /// <summary>
    /// Exercises the custody step-up and mnemonic backup zero-token overloads.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task Custody_ZeroTokenOverloads_ForwardNone()
    {
        var stepUp = new RecordingStepUpAuth();
        IStepUpAuth auth = stepUp;
        await auth.RequireAsync(AuthReason.PosPresent);

        var challenges = new RecordingStepUpChallenges();
        IStepUpChallenges prompts = challenges;
        await prompts.TryBiometricsAsync("prompt");
        await prompts.PromptForPinAsync("prompt");

        var backup = new RecordingBackupService();
        IMnemonicBackupService backups = backup;
        await backups.CreateBackupFileAsync("mnemonic", "password");
        await backups.CreateBackupFileAsync("mnemonic", "password", "hint");
        await backups.OpenBackupFileAsync(ReadOnlyMemory<byte>.Empty, "password");

        stepUp.Seen.Should().ContainSingle().Which.Should().Be(CancellationToken.None);
        challenges.Seen.Should().HaveCount(2).And.OnlyContain(t => t == CancellationToken.None);
        backup.Seen.Should().HaveCount(3).And.OnlyContain(t => t == CancellationToken.None);
    }

    /// <summary>
    /// Exercises the NFC presentment zero-token overloads and the default reader window.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task NfcPresentment_ZeroTokenOverloads_ForwardNoneAndDefaultWindow()
    {
        var stub = new RecordingNfcPresentment();
        INfcPresentmentService nfc = stub;
        var payload = new NfcPresentmentPayload { SessionId = "s", TokenRef = "t" };

        await nfc.PresentAsync(payload, CancellationToken.None);
        await nfc.PresentAsync(payload, TimeSpan.FromSeconds(5));
        await nfc.PresentAsync(payload);

        stub.Seen.Should().HaveCount(3).And.OnlyContain(t => t == CancellationToken.None);
        stub.Windows.Should().Equal(
            INfcPresentmentService.DefaultReaderWindow,
            TimeSpan.FromSeconds(5),
            INfcPresentmentService.DefaultReaderWindow);
    }

    /// <summary>
    /// Exercises the concrete zero-token overloads on the debouncer and the EMV stage simulator.
    /// Use: Low (regression gate). Scope: this fixture.
    /// </summary>
    [Fact]
    public async Task ConcreteHelpers_ZeroTokenOverloads_Run()
    {
        var debouncer = new EventDebouncer(TimeSpan.Zero);
        await debouncer.DebounceAsync(() => Task.CompletedTask);
        debouncer.FireCount.Should().Be(1);

        var stages = new List<string>();
        await foreach (string stage in EmvExchangeSimulator.RunAsync())
        {
            stages.Add(stage);
        }

        stages.Should().Equal(EmvExchangeSimulator.Stages);
    }

    /// <summary>Records the token every CancellationToken-required member receives.</summary>
    private abstract class TokenRecorder
    {
        public List<CancellationToken> Seen { get; } = new();

        /// <summary>Captures a token and completes with the type default. Use: High (per stubbed call). Scope: one fixture stub.</summary>
        protected Task<T> Record<T>(CancellationToken ct)
        {
            Seen.Add(ct);
            return Task.FromResult<T>(default!);
        }

        /// <summary>Captures a token for a non-generic member. Use: Medium (per stubbed call). Scope: one fixture stub.</summary>
        protected Task Record(CancellationToken ct)
        {
            Seen.Add(ct);
            return Task.CompletedTask;
        }
    }

    /// <summary>IProductApi stub implementing only the token-required members.</summary>
    private sealed class RecordingProductApi : TokenRecorder, IProductApi
    {
        public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct) => Record<PortfolioDto>(ct);

        public Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct)
            => Record<IReadOnlyList<HistoryPointDto>>(ct);

        public Task<SessionDto> CreateSessionAsync(CancellationToken ct) => Record<SessionDto>(ct);

        public Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
            => Record<SessionChallengeDto>(ct);

        public Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
            => Record<KeyShareResponseDto>(ct);

        public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct)
            => Record<CreateWalletResultDto>(ct);

        public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct) => Record<QuoteDto>(ct);

        public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct)
            => Record<MoneyMoveDto>(ct);

        public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct)
            => Record<MoneyMoveDto>(ct);

        public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct)
            => Record<MoneyMoveDto>(ct);

        public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct) => Record<ReceiveDto>(ct);

        public Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct)
            => Record<IReadOnlyList<VaultCardDto>>(ct);

        public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct)
            => Record<VaultCardDto>(ct);

        public Task DeleteVaultCardAsync(string cardId, CancellationToken ct) => Record(ct);

        public Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct)
            => Record<IReadOnlyList<VaultBinaryDto>>(ct);

        public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct) => Record<PosSessionDto>(ct);

        public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct) => Record<PosSessionDto>(ct);

        public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct) => Record<PosSessionDto>(ct);

        public Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct) => Record<PrefsWireDto?>(ct);

        public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct) => Record(ct);

        public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct) => Record<AccountBootstrapDto>(ct);
    }

    /// <summary>IPublicQuoteService stub implementing only the token-required members.</summary>
    private sealed class RecordingQuoteService : TokenRecorder, IPublicQuoteService
    {
        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken) => Record<bool>(cancellationToken);

        public Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken)
            => Record<IReadOnlyList<string>>(cancellationToken);

        public Task<PublicQuote> GetInverseQuoteAsync(string inputSymbol, decimal inputAmount, string outputSymbol, CancellationToken cancellationToken)
            => Record<PublicQuote>(cancellationToken);

        public Task<PublicQuote> GetQuoteAsync(string inputSymbol, decimal outputAmount, string outputSymbol, CancellationToken cancellationToken)
            => Record<PublicQuote>(cancellationToken);
    }

    /// <summary>IWalletService stub implementing only the token-required members.</summary>
    private sealed class RecordingWalletService : TokenRecorder, IWalletService
    {
        public Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken) => Record<List<Wallet>>(cancellationToken);

        public Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken) => Record<Wallet>(cancellationToken);

        public Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken) => Record<decimal>(cancellationToken);

        public Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken) => Record<Wallet>(cancellationToken);
    }

    /// <summary>IPrefsSyncService stub implementing only the token-required members.</summary>
    private sealed class RecordingPrefsSync : TokenRecorder, IPrefsSyncService
    {
        public Task PullMergeAsync(CancellationToken ct) => Record(ct);

        public Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct) => Record<bool>(ct);
    }

    /// <summary>IStepUpAuth stub implementing only the token-required member.</summary>
    private sealed class RecordingStepUpAuth : TokenRecorder, IStepUpAuth
    {
        public Task<bool> RequireAsync(AuthReason reason, CancellationToken ct) => Record<bool>(ct);
    }

    /// <summary>IStepUpChallenges stub implementing only the token-required members.</summary>
    private sealed class RecordingStepUpChallenges : TokenRecorder, IStepUpChallenges
    {
        public bool BiometricsPreferred => false;

        public Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct) => Record<bool>(ct);

        public Task<string?> PromptForPinAsync(string prompt, CancellationToken ct) => Record<string?>(ct);
    }

    /// <summary>IMnemonicBackupService stub implementing only the token-required members.</summary>
    private sealed class RecordingBackupService : TokenRecorder, IMnemonicBackupService
    {
        public Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword, CancellationToken ct)
            => Record<byte[]>(ct);

        public Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword, string? hint, CancellationToken ct)
            => Record<byte[]>(ct);

        public Task<string> OpenBackupFileAsync(ReadOnlyMemory<byte> fileBytes, string recoveryPassword, CancellationToken ct)
            => Record<string>(ct);
    }

    /// <summary>INfcPresentmentService stub recording the token and reader window it receives.</summary>
    private sealed class RecordingNfcPresentment : TokenRecorder, INfcPresentmentService
    {
        public bool IsSupported => true;

        public string? LastError => null;

        public List<TimeSpan> Windows { get; } = new();

        public Task<bool> PresentAsync(NfcPresentmentPayload payload, TimeSpan timeout, CancellationToken ct)
        {
            Windows.Add(timeout);
            return Record<bool>(ct);
        }
    }
}
