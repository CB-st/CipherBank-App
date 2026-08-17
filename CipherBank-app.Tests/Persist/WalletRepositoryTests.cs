// <copyright file="WalletRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class WalletRepositoryTests
{
    /// <summary>
    /// Upserts two wallets, lists them ordered by CreatedAt, then deletes one.
    /// Use: Medium (coverage / Persist gate). Scope: WalletRepositoryTests.
    /// </summary>
    [Fact]
    public async Task UpsertListDelete_RoundTripsRows()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-wallet-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        WalletRepository repo = new WalletRepository(db);

        DateTimeOffset earlier = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset later = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        LocalWalletRow first = HdWallet("w1", "BTC", "Primary", "bc1qexample", "m/84'/0'/0'/0/0", earlier);
        LocalWalletRow second = HdWallet("w2", "ETH", "Secondary", "0xabc", "m/44'/60'/0'/0/0", later);

        await repo.UpsertAsync(first);
        await repo.UpsertAsync(second);
        await repo.UpsertAsync(first with { Label = "Primary renamed" });

        IReadOnlyList<LocalWalletRow> listed = await repo.ListAsync();
        listed.Should().HaveCount(2);
        listed[0].Id.Should().Be("w1");
        listed[0].Label.Should().Be("Primary renamed");
        listed[1].Id.Should().Be("w2");

        await repo.DeleteAsync("w1");
        await repo.DeleteAsync("missing");

        (await repo.ListAsync()).Should().ContainSingle().Which.Id.Should().Be("w2");
    }

    private static LocalWalletRow HdWallet(
        string id,
        string symbol,
        string label,
        string address,
        string derivationPath,
        DateTimeOffset createdAt)
        => new(
            id,
            symbol,
            label,
            address,
            derivationPath,
            AccountIndex: 0,
            Kind: "hd",
            createdAt);
}
