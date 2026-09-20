// <copyright file="CertificatePinningTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Tests for certificate pinning and platform handler functionality.
/// Note: Full certificate pinning tests require platform-specific testing.
/// </summary>
public class CertificatePinningTests
{
    [Fact]
    public void HttpClientHandler_CanBeCreated()
    {
        // Arrange & Act - Basic handler creation test
        var handler = new HttpClientHandler();

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeAssignableTo<HttpMessageHandler>();
    }
}
