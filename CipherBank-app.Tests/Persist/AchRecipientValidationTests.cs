// <copyright file="AchRecipientValidationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class AchRecipientValidationTests
{
    [Fact]
    public void Validate_AcceptsCompleteCheckingAccount()
    {
        AchRecipientValidation.Validate(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "021000021",
            "12345678",
            "checking",
            "April rent").Should().BeNull();
    }

    [Fact]
    public void Validate_RejectsShortRouting()
    {
        AchRecipientValidation.Validate(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "02100",
            "12345678",
            "checking").Should().Contain("9 digits");
    }

    [Fact]
    public void Validate_RejectsRoutingWithNonDigitCharacters()
    {
        AchRecipientValidation.Validate(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "abc021000021",
            "12345678",
            "checking").Should().Contain("9 digits");
    }

    [Fact]
    public void MaskAccount_KeepsLastFour() => AchRecipientValidation.MaskAccount("88210001").Should().Be("•••• 0001");
}
