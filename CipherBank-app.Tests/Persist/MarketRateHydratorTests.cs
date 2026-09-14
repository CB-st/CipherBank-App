// <copyright file="MarketRateHydratorTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class MarketRateHydratorTests
{
    [Fact]
    public void FromQuote_MapsInverseQuoteRateAndTimestamp()
    {
        PublicQuote quote = new("btc", 1m, "USD", 67_123.45m);

        RateRow row = RateRow.FromQuote(quote, updatedAtMs: 1_000);

        row.Should().Be(new RateRow("BTC", 67_123.45m, 0m, 1_000));
    }

    [Fact]
    public async Task HydrateAndRefreshAsync_RefreshesWhenCachedTimestampIsInTheFuture()
    {
        DateTimeOffset now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        long nowMs = now.ToUnixTimeMilliseconds();
        MemoryRatesCache cache = new();
        cache.Seed(new RateRow("BTC", 1m, 0m, nowMs + (long)TimeSpan.FromHours(1).TotalMilliseconds));
        CountingQuoteService quotes = new();
        MarketRateHydrator hydrator = new(
            cache,
            quotes,
            new FixedTimeProvider(now));

        await hydrator.HydrateAndRefreshAsync([" btc "], CancellationToken.None);

        quotes.InverseQuoteCalls.Should().Be(1);
        cache.UpsertCalls.Should().Be(1);
        cache.Rows["BTC"].UpdatedAtMs.Should().Be(nowMs);
    }

    [Fact]
    public async Task HydrateAndRefreshAsync_ReusesRowYoungerThanMaxAge()
    {
        DateTimeOffset now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        long nowMs = now.ToUnixTimeMilliseconds();
        MemoryRatesCache cache = new();
        cache.Seed(new RateRow("BTC", 1m, 0m, nowMs - (long)TimeSpan.FromMinutes(1).TotalMilliseconds));
        CountingQuoteService quotes = new();
        MarketRateHydrator hydrator = new(
            cache,
            quotes,
            new FixedTimeProvider(now));

        await hydrator.HydrateAndRefreshAsync(["BTC"], CancellationToken.None);

        quotes.InverseQuoteCalls.Should().Be(0);
        cache.UpsertCalls.Should().Be(0);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }

    private sealed class MemoryRatesCache : IRatesCache
    {
        public Dictionary<string, RateRow> Rows { get; } = new Dictionary<string, RateRow>(StringComparer.Ordinal);

        public int UpsertCalls { get; private set; }

        public void Seed(RateRow row) => Rows[row.Symbol] = row;

        public Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct)
        {
            UpsertCalls++;
            foreach (RateRow row in rows)
            {
                Rows[row.Symbol] = row;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RateRow>> GetAsync(IEnumerable<string>? symbols, CancellationToken ct)
        {
            if (symbols is null)
            {
                return Task.FromResult<IReadOnlyList<RateRow>>(Rows.Values.ToList());
            }

            List<RateRow> matched = new();
            foreach (string symbol in symbols)
            {
                if (Rows.TryGetValue(symbol, out RateRow? row))
                {
                    matched.Add(row);
                }
            }

            return Task.FromResult<IReadOnlyList<RateRow>>(matched);
        }
    }

    private sealed class CountingQuoteService : IPublicQuoteService
    {
        public int InverseQuoteCalls { get; private set; }

        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>(["BTC"]);

        public Task<PublicQuote> GetInverseQuoteAsync(
            string inputSymbol,
            decimal inputAmount,
            string outputSymbol,
            CancellationToken cancellationToken)
        {
            InverseQuoteCalls++;
            return Task.FromResult(new PublicQuote(inputSymbol, inputAmount, outputSymbol, 50_000m));
        }

        public Task<PublicQuote> GetQuoteAsync(
            string inputSymbol,
            decimal outputAmount,
            string outputSymbol,
            CancellationToken cancellationToken)
            => Task.FromResult(new PublicQuote(inputSymbol, 1m, outputSymbol, outputAmount));
    }
}
