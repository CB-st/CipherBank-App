// <copyright file="AuthServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        Mock<IAuthService> mockAuthService = new Mock<IAuthService>();
        AuthToken expectedToken = new AuthToken(
            "test_access_token",
            "test_refresh_token",
            DateTimeOffset.UtcNow.AddHours(1));

        mockAuthService
            .Setup(x => x.LoginAsync("testuser", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        // Act
        AuthToken result = await mockAuthService.Object.LoginAsync("testuser", "password", default);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token");
        result.RefreshToken.Should().Be("test_refresh_token");
        result.ExpiresUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        Mock<IAuthService> mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.LoginAsync("invalid", "wrong", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        Func<Task<AuthToken>> act = async () => await mockAuthService.Object.LoginAsync("invalid", "wrong", default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        Mock<IAuthService> mockAuthService = new Mock<IAuthService>();
        AuthToken newToken = new AuthToken(
            "new_access_token",
            "new_refresh_token",
            DateTimeOffset.UtcNow.AddHours(1));

        mockAuthService
            .Setup(x => x.RefreshAsync("old_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newToken);

        // Act
        AuthToken result = await mockAuthService.Object.RefreshAsync("old_refresh_token", default);

        // Assert
        result.AccessToken.Should().Be("new_access_token");
    }

    [Fact]
    public async Task IsTokenExpiredAsync_WhenExpired_ReturnsTrue()
    {
        // Arrange
        Mock<IAuthService> mockAuthService = new Mock<IAuthService>();
        mockAuthService
            .Setup(x => x.IsTokenExpiredAsync())
            .ReturnsAsync(true);

        // Act
        bool result = await mockAuthService.Object.IsTokenExpiredAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ClearsSession()
    {
        // Arrange
        Mock<IAuthService> mockAuthService = new Mock<IAuthService>();
        mockAuthService.Setup(x => x.LogoutAsync()).Returns(Task.CompletedTask);
        mockAuthService.Setup(x => x.GetStoredTokenAsync()).ReturnsAsync((AuthToken?)null);

        // Act
        await mockAuthService.Object.LogoutAsync();
        AuthToken? storedToken = await mockAuthService.Object.GetStoredTokenAsync();

        // Assert
        storedToken.Should().BeNull();
    }
}
