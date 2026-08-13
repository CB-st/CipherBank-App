// <copyright file="NfcPresentmentPayloadTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Pos;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Pos;

public class NfcPresentmentPayloadTests
{
    [Fact]
    public void ToJson_RoundTripsWithoutPan()
    {
        NfcPresentmentPayload payload = new NfcPresentmentPayload
        {
            SessionId = "sess_abc",
            TokenRef = "tok_xyz",
            MerchantId = "m1",
        };
        string json = payload.ToJson();
        json.Should().Contain("sessionId");
        json.Should().Contain("tokenRef");
        json.Should().NotContain("PAN");
        json.Should().NotContain("pan");

        NfcPresentmentPayload? parsed = NfcPresentmentPayload.TryParse(json);
        parsed.Should().NotBeNull();
        parsed!.SessionId.Should().Be("sess_abc");
        parsed.TokenRef.Should().Be("tok_xyz");
        parsed.MerchantId.Should().Be("m1");
    }

    [Fact]
    public void TryParse_TypeInvalidJson_ReturnsNull()
    {
        NfcPresentmentPayload.TryParse("""{"v":"1"}""").Should().BeNull();
        NfcPresentmentPayload.TryParse("""{"tokenRef":42}""").Should().BeNull();
    }
}
