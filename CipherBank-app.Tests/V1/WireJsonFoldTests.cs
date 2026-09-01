// <copyright file="WireJsonFoldTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class WireJsonFoldTests
{
    [Fact]
    public void PrefsWireDto_FoldsCamelCaseExtensionData()
    {
        const string json = """{"coraEnabled":false,"assetsLayout":"combined","appLockIdleSec":90}""";
        PrefsWireDto? dto = JsonSerializer.Deserialize<PrefsWireDto>(json);
        dto.Should().NotBeNull();
        UserPrefs prefs = new UserPrefs();
        dto!.ApplyOnto(prefs);
        prefs.CoraEnabled.Should().BeFalse();
        prefs.AssetsLayout.Should().Be("combined");
        prefs.LockIdleSeconds.Should().Be(90);
    }

    [Fact]
    public void AccountBootstrapDto_FoldsCamelRecipients()
    {
        const string json = """
            {"prefs":{"defaultSendSpeed":"ach"},"recipients":[{"id":"r1","displayName":"Ada","accountType":"savings"}],"syncedAt":123}
            """;
        AccountBootstrapDto? dto = JsonSerializer.Deserialize<AccountBootstrapDto>(json);
        dto.Should().NotBeNull();
        dto!.ResolvedPrefs!.DefaultSendSpeed.Should().Be("ach");
        dto.ResolvedRecipients.Should().ContainSingle();
        BootstrapRecipientDto contact = dto.ResolvedRecipients[0];
        contact.ResolvedId.Should().Be("r1");
        contact.ResolvedName.Should().Be("Ada");
        contact.ResolvedAccountType.Should().Be("savings");
        dto.SyncedAt.Should().Be(123);
    }

    [Fact]
    public void AccountBootstrapDto_PopulatesUppercaseRecipients()
    {
        const string json = """
            {"PREFS":{"DEFAULT_SEND_SPEED":"wire"},"RECIPIENTS":[{"ID":"r2","DISPLAY_NAME":"Bob","ACCOUNT_TYPE":"checking","ROUTING_NUMBER":"021000021","ACCOUNT_LAST4":"1234"}],"SYNCED_AT":99}
            """;
        AccountBootstrapDto? dto = JsonSerializer.Deserialize<AccountBootstrapDto>(json);
        dto.Should().NotBeNull();
        dto!.ResolvedRecipients.Should().ContainSingle();
        dto.ResolvedRecipients[0].ResolvedId.Should().Be("r2");
        dto.ResolvedRecipients[0].ResolvedName.Should().Be("Bob");
        dto.SyncedAt.Should().Be(99);
    }

    [Fact]
    public void BootstrapRecipientDto_SyntheticIdIncludesAccountIdentity()
    {
        BootstrapRecipientDto a = new BootstrapRecipientDto
        {
            DisplayName = "Same Name",
            RoutingNumber = "021000021",
            AccountLast4 = "1111",
        };
        BootstrapRecipientDto b = new BootstrapRecipientDto
        {
            DisplayName = "Same Name",
            RoutingNumber = "021000021",
            AccountLast4 = "2222",
        };

        a.ResolvedId.Should().NotBe(b.ResolvedId);
        a.ResolvedId.Should().StartWith("bootstrap_");
    }
}
