// <copyright file="ClientWebSocketStreamServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class ClientWebSocketStreamServiceTests
{
    /// <summary>
    /// Constructs from Uri/string and disconnects while never connected.
    /// Use: Medium (dispose safety / coverage). Scope: ClientWebSocketStreamServiceTests.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_WhenNeverConnected_IsIdempotent()
    {
        await using ClientWebSocketStreamService fromUri = new ClientWebSocketStreamService(
            new Uri("wss://example.invalid/stream"));
        await using ClientWebSocketStreamService fromString = new ClientWebSocketStreamService(
            "wss://example.invalid/stream");

        fromUri.IsConnected.Should().BeFalse();
        fromString.IsConnected.Should().BeFalse();

        await fromUri.DisconnectAsync();
        await fromUri.DisconnectAsync();
        await fromString.DisconnectAsync();
    }

    /// <summary>
    /// Rejects a null URI at construction.
    /// Use: Low. Scope: ClientWebSocketStreamServiceTests guards.
    /// </summary>
    [Fact]
    public void Constructor_NullUri_Throws()
    {
        Action act = () => _ = new ClientWebSocketStreamService((Uri)null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
