using CipherBank_app.Services.Logging;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Unit tests for the LogRedactionHelper class.
/// </summary>
public class LogRedactionHelperTests
{
    #region RedactUsername Tests

    [Fact]
    public void RedactUsername_StandardUsername_RedactsMiddle()
    {
        // Arrange
        var username = "testuser123";

        // Act
        var redacted = LogRedactionHelper.RedactUsername(username);

        // Assert
        redacted.Should().NotBe(username);
        redacted.Should().StartWith("t");
        redacted.Should().EndWith("3");
        redacted.Should().Contain("*");
        redacted.Length.Should().Be(username.Length);
    }

    [Fact]
    public void RedactUsername_ShortUsername_ReturnsRedactionMarker()
    {
        // Arrange
        var username = "ab";

        // Act
        var redacted = LogRedactionHelper.RedactUsername(username);

        // Assert
        redacted.Should().Be("***");
    }

    [Fact]
    public void RedactUsername_NullUsername_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactUsername(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    [Fact]
    public void RedactUsername_EmptyUsername_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactUsername("");

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region RedactWalletId Tests

    [Fact]
    public void RedactWalletId_StandardWalletId_RedactsMiddle()
    {
        // Arrange
        var walletId = "wallet1234567890abcdef";

        // Act
        var redacted = LogRedactionHelper.RedactWalletId(walletId);

        // Assert
        redacted.Should().NotBe(walletId);
        redacted.Should().StartWith("wall");
        redacted.Should().EndWith("cdef");
        redacted.Should().Contain("...");
        redacted.Length.Should().BeLessThan(walletId.Length);
    }

    [Fact]
    public void RedactWalletId_ShortWalletId_RedactsPartially()
    {
        // Arrange
        var walletId = "short";

        // Act
        var redacted = LogRedactionHelper.RedactWalletId(walletId);

        // Assert
        redacted.Should().StartWith("sh");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactWalletId_NullWalletId_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactWalletId(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region RedactAddress Tests

    [Fact]
    public void RedactAddress_BitcoinAddress_RedactsMiddle()
    {
        // Arrange
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";

        // Act
        var redacted = LogRedactionHelper.RedactAddress(address);

        // Assert
        redacted.Should().NotBe(address);
        redacted.Should().StartWith("1A1zP1");
        redacted.Should().EndWith("fNa");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactAddress_EthereumAddress_RedactsMiddle()
    {
        // Arrange
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0";

        // Act
        var redacted = LogRedactionHelper.RedactAddress(address);

        // Assert
        redacted.Should().NotBe(address);
        redacted.Should().StartWith("0x742d");
        redacted.Should().EndWith("bEb0");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactAddress_ShortAddress_RedactsPartially()
    {
        // Arrange
        var address = "short";

        // Act
        var redacted = LogRedactionHelper.RedactAddress(address);

        // Assert
        redacted.Should().StartWith("sho");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactAddress_NullAddress_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactAddress(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region RedactToken Tests

    [Fact]
    public void RedactToken_JwtToken_RedactsAfterPrefix()
    {
        // Arrange
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        // Act
        var redacted = LogRedactionHelper.RedactToken(token);

        // Assert
        redacted.Should().NotBe(token);
        redacted.Should().StartWith("eyJhbGci");
        redacted.Should().EndWith("...");
        redacted.Length.Should().BeLessThan(token.Length);
    }

    [Fact]
    public void RedactToken_ShortToken_ReturnsRedactionMarker()
    {
        // Arrange
        var token = "short";

        // Act
        var redacted = LogRedactionHelper.RedactToken(token);

        // Assert
        redacted.Should().Be("***");
    }

    [Fact]
    public void RedactToken_NullToken_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactToken(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region RedactEmail Tests

    [Fact]
    public void RedactEmail_StandardEmail_RedactsLocalPart()
    {
        // Arrange
        var email = "user@example.com";

        // Act
        var redacted = LogRedactionHelper.RedactEmail(email);

        // Assert
        redacted.Should().NotBe(email);
        redacted.Should().StartWith("us");
        redacted.Should().Contain("***");
        redacted.Should().EndWith("@example.com");
    }

    [Fact]
    public void RedactEmail_ShortLocalPart_RedactsWithMarker()
    {
        // Arrange
        var email = "a@example.com";

        // Act
        var redacted = LogRedactionHelper.RedactEmail(email);

        // Assert
        redacted.Should().Contain("***");
        redacted.Should().EndWith("@example.com");
    }

    [Fact]
    public void RedactEmail_InvalidEmail_ReturnsRedactionMarker()
    {
        // Arrange
        var email = "notanemail";

        // Act
        var redacted = LogRedactionHelper.RedactEmail(email);

        // Assert
        redacted.Should().Be("***");
    }

    [Fact]
    public void RedactEmail_NullEmail_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactEmail(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region RedactTransactionId Tests

    [Fact]
    public void RedactTransactionId_StandardId_RedactsMiddle()
    {
        // Arrange
        var transactionId = "tx_1234567890abcdef";

        // Act
        var redacted = LogRedactionHelper.RedactTransactionId(transactionId);

        // Assert
        redacted.Should().NotBe(transactionId);
        redacted.Should().StartWith("tx_12345");
        redacted.Should().EndWith("cdef");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactTransactionId_ShortId_RedactsPartially()
    {
        // Arrange
        var transactionId = "tx_123";

        // Act
        var redacted = LogRedactionHelper.RedactTransactionId(transactionId);

        // Assert
        redacted.Should().StartWith("tx_1");
        redacted.Should().Contain("...");
    }

    [Fact]
    public void RedactTransactionId_NullId_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.RedactTransactionId(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion

    #region Generic Redact Tests

    [Theory]
    [InlineData("abcdefghijklmnop", 4, "abcd...mnop")]
    [InlineData("short", 4, "***")]
    [InlineData("12345678", 2, "12...78")]
    public void Redact_WithCustomShowChars_RedactsCorrectly(string value, int showChars, string expected)
    {
        // Act
        var redacted = LogRedactionHelper.Redact(value, showChars);

        // Assert
        redacted.Should().Be(expected);
    }

    [Fact]
    public void Redact_NullValue_ReturnsEmpty()
    {
        // Act
        var redacted = LogRedactionHelper.Redact(null);

        // Assert
        redacted.Should().Be("[empty]");
    }

    #endregion
}
