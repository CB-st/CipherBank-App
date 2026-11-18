// <copyright file="NfcPresentmentPayloadTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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

    /// <summary>
    /// Empty objects, unsupported versions, and blank identifiers are malformed.
    /// Use: Medium (NFC). Scope: NfcPresentmentPayload.TryParse.
    /// </summary>
    [Fact]
    public void TryParse_RejectsMissingIdentifiersAndUnsupportedVersion()
    {
        NfcPresentmentPayload.TryParse("{}").Should().BeNull();
        NfcPresentmentPayload.TryParse("""{"v":2}""").Should().BeNull();
        NfcPresentmentPayload.TryParse("""{"v":2,"sessionId":"sess","tokenRef":"tok"}""").Should().BeNull();
        NfcPresentmentPayload.TryParse("""{"v":1,"sessionId":"","tokenRef":"tok"}""").Should().BeNull();
        NfcPresentmentPayload.TryParse("""{"v":1,"sessionId":"sess","tokenRef":" "}""").Should().BeNull();

        NfcPresentmentPayload? parsed = NfcPresentmentPayload.TryParse(
            """{"sessionId":"sess","tokenRef":"tok"}""");
        parsed.Should().NotBeNull();
        parsed!.V.Should().Be(NfcPresentmentPayload.SupportedVersion);
        parsed.SessionId.Should().Be("sess");
        parsed.TokenRef.Should().Be("tok");
    }

    /// <summary>
    /// Payloads where one identifier key is absent, null, or the wrong JSON type are malformed.
    /// Use: Medium (NFC). Scope: NfcPresentmentPayload.TryParse.
    /// </summary>
    [Theory]
    [InlineData("""{"v":1,"sessionId":"sess"}""")]
    [InlineData("""{"v":1,"tokenRef":"tok"}""")]
    [InlineData("""{"v":1,"sessionId":null,"tokenRef":"tok"}""")]
    [InlineData("""{"v":1,"sessionId":"sess","tokenRef":null}""")]
    [InlineData("""{"v":1,"sessionId":"\t","tokenRef":"tok"}""")]
    public void TryParse_RejectsMissingOrNullIdentifier(string json)
        => NfcPresentmentPayload.TryParse(json).Should().BeNull();

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{\"v\":null,\"sessionId\":\"sess\",\"tokenRef\":\"tok\"}")]
    [InlineData("{\"sessionId\":\"sess\",\"tokenRef\":\"tok\",\"merchantId\":42}")]
    public void TryParse_RejectsInvalidJsonShape(string json)
        => NfcPresentmentPayload.TryParse(json).Should().BeNull();
}
