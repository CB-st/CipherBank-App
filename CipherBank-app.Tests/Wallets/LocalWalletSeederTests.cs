// <copyright file="LocalWalletSeederTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
        wallets.Rows.Add(new LocalWalletRow(
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

        LocalWalletRow row = wallets.Rows.Should().ContainSingle(r => r.Id == "old-btc").Subject;
        row.Address.Should().NotBe("old-address-should-be-replaced");
        row.Address.Should().NotBeNullOrWhiteSpace();
        row.Kind.Should().Be("derived");
    }

    private sealed class FakeWallets : IWalletRepository
    {
        public List<LocalWalletRow> Rows { get; } = [];

        public Task<IReadOnlyList<LocalWalletRow>> ListAsync()
            => Task.FromResult<IReadOnlyList<LocalWalletRow>>(Rows);

        public Task UpsertAsync(LocalWalletRow row)
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
    }
}
