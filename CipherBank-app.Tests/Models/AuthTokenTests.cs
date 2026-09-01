// <copyright file="AuthTokenTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public sealed class AuthTokenTests
{
    [Fact]
    public void Record_PreservesAccessRefreshAndExpiry()
    {
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddHours(1);
        AuthToken token = new("access", "refresh", expires);

        token.AccessToken.Should().Be("access");
        token.RefreshToken.Should().Be("refresh");
        token.ExpiresUtc.Should().Be(expires);
        token.ExpiresUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
