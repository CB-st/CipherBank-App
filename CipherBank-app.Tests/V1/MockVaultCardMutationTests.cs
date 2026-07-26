// <copyright file="MockVaultCardMutationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class MockVaultCardMutationTests
{
    [Fact]
    public async Task Add_vault_card_persists_returned_card_metadata()
    {
        var api = new MockProductApi();

        VaultCardDto added = await api.AddVaultCardAsync(
            new VaultCardDto
            {
                Last4 = "9876",
                Brand = "mastercard",
                Label = "Travel card",
                HardwareTest = false,
            },
            "add-card-1");

        IReadOnlyList<VaultCardDto> cards = await api.GetVaultCardsAsync();

        added.CardId.Should().StartWith("card_");
        cards.Should().ContainSingle(card =>
            card.CardId == added.CardId &&
            card.Last4 == "9876" &&
            card.Brand == "mastercard" &&
            card.Label == "Travel card" &&
            !card.HardwareTest);
    }

    [Fact]
    public async Task Delete_vault_card_removes_only_requested_card()
    {
        var api = new MockProductApi();
        VaultCardDto added = await api.AddVaultCardAsync(
            new VaultCardDto { Last4 = "9876", Brand = "mastercard", Label = "Travel card", HardwareTest = false },
            "delete-card-1");

        await api.DeleteVaultCardAsync(added.CardId);

        IReadOnlyList<VaultCardDto> cards = await api.GetVaultCardsAsync();
        cards.Should().NotContain(card => card.CardId == added.CardId);
        cards.Should().Contain(card => card.CardId == "card_lab_1");
    }
}
