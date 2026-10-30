// <copyright file="LocalWalletSeederTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

public class LocalWalletSeederTests
{
    [Fact]
    public async Task EnsureDerivedAsync_UpdatesAddressWhenDerivedRowExistsForDifferentSeed()
    {
        FakeWallets wallets = new FakeWallets();
        wallets.Rows.Add(new LocalWalletDescriptor(
            "old-btc",
            "BTC",
            "BTC Primary",
            "old-address-should-be-replaced",
            "m/84'/0'/0'/0/0",
            0,
            "derived",
            DateTimeOffset.UtcNow));

        LocalWalletSeeder seeder = new LocalWalletSeeder(wallets);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        await seeder.EnsureDerivedAsync(mnemonic, ["BTC"]);

        LocalWalletDescriptor row = wallets.Rows.Should().ContainSingle(r => r.Id == "old-btc").Subject;
        row.Address.Should().NotBe("old-address-should-be-replaced");
        row.Address.Should().NotBeNullOrWhiteSpace();
        row.Kind.Should().Be("derived");
    }

    /// <summary>A null symbol is rejected before any wallet is written. Use: Medium. Scope: LocalWalletSeeder.</summary>
    [Fact]
    public async Task EnsureDerivedAsync_RejectsNullSymbol()
    {
        FakeWallets wallets = new FakeWallets();
        LocalWalletSeeder seeder = new LocalWalletSeeder(wallets);

        Func<Task> act = () => seeder.EnsureDerivedAsync("abandon about", [new AssetSymbol("BTC"), null!]);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("symbols");
        wallets.Rows.Should().BeEmpty();
    }

    /// <summary>
    /// BTC and btc are one normalized symbol, so seeding inserts a single derived row.
    /// Use: Medium (seed). Scope: LocalWalletSeeder.
    /// </summary>
    [Fact]
    public async Task EnsureDerivedAsync_DeduplicatesNormalizedSymbols()
    {
        FakeWallets wallets = new FakeWallets();
        LocalWalletSeeder seeder = new LocalWalletSeeder(wallets);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        await seeder.EnsureDerivedAsync(mnemonic, ["BTC", "btc", " BTC "]);

        LocalWalletDescriptor row = wallets.Rows.Should().ContainSingle().Subject;
        row.Symbol.Should().Be(new AssetSymbol("BTC"));
        row.Kind.Should().Be("derived");
        row.Address.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Mixed-case duplicates across several assets seed one row per asset and skip non-derivable ones.
    /// Use: Medium (seed). Scope: LocalWalletSeeder.
    /// </summary>
    [Fact]
    public async Task EnsureDerivedAsync_DeduplicatesAcrossAssetsAndSkipsNonDerivable()
    {
        FakeWallets wallets = new FakeWallets();
        LocalWalletSeeder seeder = new LocalWalletSeeder(wallets);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        await seeder.EnsureDerivedAsync(mnemonic, ["btc", "ETH", "eth", "BTC", "xmr"]);
        await seeder.EnsureDerivedAsync(mnemonic, ["BTC", "ETH"]);

        wallets.Rows.Select(r => r.Symbol).Should().BeEquivalentTo([AssetSymbol.Btc, AssetSymbol.Eth]);
    }

    private sealed class FakeWallets : IWalletRepository
    {
        public List<LocalWalletDescriptor> Rows { get; } = [];

        public Task<IReadOnlyList<LocalWalletDescriptor>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LocalWalletDescriptor>>(Rows);

        public Task UpsertAsync(LocalWalletDescriptor row, CancellationToken ct = default)
        {
            Rows.RemoveAll(r => r.Id == row.Id);
            Rows.Add(row);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }
    }
}
