// <copyright file="WalletRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        LocalDb db = new(new FileInfo(path));
        await db.InitializeAsync();
        WalletRepository repo = new(db);

        DateTimeOffset earlier = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset later = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
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

    /// <summary>
    /// Repository reads must propagate caller cancellation into database initialization and EF.
    /// Use: Medium (navigation cancellation). Scope: WalletRepository.
    /// </summary>
    [Fact]
    public async Task ListAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-wallet-" + Guid.NewGuid().ToString("N") + ".db");
        WalletRepository repo = new(new LocalDb(new FileInfo(path)));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        Func<Task> act = async () => await repo.ListAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
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
