// <copyright file="CreateWalletTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class CreateWalletTests
{
    [Fact]
    public async Task Mock_managed_xmr_returns_wallet_without_spend_key_fields()
    {
        var api = new MockProductApi();
        CreateWalletResultDto result = await api.CreateWalletAsync(
            new CreateWalletRequestDto
            {
                Symbol = "XMR",
                Label = "Managed",
                Mode = "managed",
            },
            default);

        result.WalletId.Should().StartWith("wlt_");
        result.Mode.Should().Be("managed");
        result.Address.Should().NotBeNullOrWhiteSpace();
        string json = System.Text.Json.JsonSerializer.Serialize(result).ToLowerInvariant();
        json.Should().NotContain("spend");
        json.Should().NotContain("viewkey");
        json.Should().NotContain("mnemonic");
    }
}
