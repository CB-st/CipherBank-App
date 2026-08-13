// <copyright file="InMemoryVaultCardMutationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class InMemoryVaultCardMutationTests
{
    [Fact]
    public async Task Add_vault_card_persists_returned_card_metadata()
    {
        InMemoryProductClient api = new InMemoryProductClient();

        VaultCardDto added = await api.AddVaultCardAsync(
            new VaultCardDto
            {
                Last4 = "9876",
                Brand = "mastercard",
                Label = "Travel card",
                HardwareTest = false,
            },
            "add-card-1",
            default);

        IReadOnlyList<VaultCardDto> cards = await api.GetVaultCardsAsync(default);

        added.CardId.Should().StartWith("card_");
        VaultCardDto match = cards.Should().ContainSingle(card => card.CardId == added.CardId).Subject;
        match.Last4.Should().Be("9876");
        match.Brand.Should().Be("mastercard");
        match.Label.Should().Be("Travel card");
        match.HardwareTest.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_vault_card_removes_only_requested_card()
    {
        InMemoryProductClient api = new InMemoryProductClient();
        VaultCardDto added = await api.AddVaultCardAsync(
            new VaultCardDto { Last4 = "9876", Brand = "mastercard", Label = "Travel card", HardwareTest = false },
            "delete-card-1",
            default);

        await api.DeleteVaultCardAsync(added.CardId, default);

        IReadOnlyList<VaultCardDto> cards = await api.GetVaultCardsAsync(default);
        cards.Should().NotContain(card => card.CardId == added.CardId);
        cards.Should().Contain(card => card.CardId == "card_lab_1");
    }
}
