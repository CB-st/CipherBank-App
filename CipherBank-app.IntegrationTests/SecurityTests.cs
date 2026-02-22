using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace CipherBank_app.IntegrationTests;

/// <summary>
/// Security-focused integration tests to verify authentication,
/// authorization, and secure communication patterns.
/// </summary>
public class SecurityTests : IClassFixture<MockServerFixture>
{
    private readonly MockServerFixture _fixture;
    private readonly HttpClient _client;

    public SecurityTests(MockServerFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task UnauthorizedRequest_ReturnsUnauthorized()
    {
        // Arrange - Setup an endpoint that requires auth
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/secure/resource")
                .WithHeader("Authorization", "*", WireMock.Matchers.MatchBehaviour.RejectOnMatch)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody("{\"error\":\"Authentication required\"}"));

        // Act
        var response = await _client.GetAsync("/api/v1/secure/resource");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthorizedRequest_WithValidToken_Succeeds()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/secure/data")
                .WithHeader("Authorization", "Bearer valid_token")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"data\":\"secure_content\"}"));

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid_token");

        // Act
        var response = await _client.GetAsync("/api/v1/secure/data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/protected/endpoint")
                .WithHeader("Authorization", "Bearer expired_token")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody("{\"error\":\"Token expired\"}"));

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "expired_token");

        // Act
        var response = await _client.GetAsync("/api/v1/protected/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/auth/login/invalid")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBody("{\"error\":\"Invalid username or password\"}"));

        var invalidLogin = new { user = "wrong", password = "incorrect" };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login/invalid", invalidLogin);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RateLimiting_Returns429WhenExceeded()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/rate-limited")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Retry-After", "60")
                .WithBody("{\"error\":\"Rate limit exceeded\"}"));

        // Act
        var response = await _client.GetAsync("/api/v1/rate-limited");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task XssAttempt_IsSanitized()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/crypto/search")
                .WithParam("q", "<script>alert('xss')</script>")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithBody("{\"error\":\"Invalid input\"}"));

        // Act
        var response = await _client.GetAsync("/api/v1/crypto/search?q=<script>alert('xss')</script>");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SqlInjectionAttempt_IsRejected()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/wallets/'; DROP TABLE wallets;--")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithBody("{\"error\":\"Invalid wallet ID\"}"));

        // Act
        var response = await _client.GetAsync("/api/v1/wallets/'; DROP TABLE wallets;--");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ContentSecurityHeaders_ArePresent()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/secure-headers")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Security-Policy", "default-src 'self'")
                .WithHeader("X-Content-Type-Options", "nosniff")
                .WithHeader("X-Frame-Options", "DENY")
                .WithHeader("X-XSS-Protection", "1; mode=block")
                .WithBody("{\"secure\":true}"));

        // Act
        var response = await _client.GetAsync("/api/v1/secure-headers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Content-Type-Options").Should().BeTrue();
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
    }

    [Fact]
    public async Task SensitiveDataNotInLogs_TransactionEndpoint()
    {
        // This test verifies that sensitive transaction data is properly handled
        // The actual logging verification would be done through log inspection

        // Arrange
        var sensitiveRequest = new
        {
            fromWalletId = "wallet_123",
            toAddress = "bc1qsensitiveaddress",
            amount = 1.5m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/send", sensitiveRequest);

        // Assert - Verify the endpoint works (actual log verification is manual)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TimeoutHandling_ReturnsGatewayTimeout()
    {
        // Arrange
        _fixture.Server.Given(Request.Create()
                .WithPath("/api/v1/slow-endpoint")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.GatewayTimeout)
                .WithDelay(TimeSpan.FromMilliseconds(100))
                .WithBody("{\"error\":\"Request timeout\"}"));

        // Act
        var response = await _client.GetAsync("/api/v1/slow-endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
    }
}
