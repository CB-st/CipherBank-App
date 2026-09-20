// <copyright file="AchRecipientValidationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Resources;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class AchRecipientValidationTests
{
    /// <summary>
    /// Every simultaneous failure is reported, in stable field order:
    /// name, holder, bank, routing, account, account type, memo.
    /// Use: Medium (multi-error contract regression). Scope: AchRecipientValidation.
    /// </summary>
    [Fact]
    public void ValidateAll_ReportsEveryFailureInFieldOrder()
    {
        IReadOnlyList<string> errors = AchRecipientValidation.ValidateAll(
            " ",
            " ",
            "Demo Bank",
            "02100",
            "12",
            "moneymarket",
            new string('x', AchRecipientValidation.MemoMaxLength + 1));

        errors.Should().Equal(
            UserFacingStrings.AchEnterPayeeName,
            UserFacingStrings.AchEnterAccountHolderName,
            UserFacingStrings.AchRoutingNumberMustBeDigits(AchRecipientValidation.RoutingNumberDigitCount),
            UserFacingStrings.AchEnterValidAccountNumber,
            UserFacingStrings.AchAccountTypeMustBeCheckingOrSavings,
            UserFacingStrings.AchMemoMustBeMaxLength(AchRecipientValidation.MemoMaxLength));
    }

    /// <summary>
    /// The detailed variant carries the failing field with each message so consumers can
    /// attach errors to their inputs; order and messages match <c>ValidateAll</c>.
    /// Use: Medium (structured-error contract regression). Scope: AchRecipientValidation.
    /// </summary>
    [Fact]
    public void ValidateDetailed_ReportsFieldAndMessageForEveryFailure()
    {
        IReadOnlyList<AchValidationError> errors = AchRecipientValidation.ValidateDetailed(
            " ",
            " ",
            "Demo Bank",
            "02100",
            "12",
            "moneymarket",
            new string('x', AchRecipientValidation.MemoMaxLength + 1));

        errors.Select(static e => e.Field).Should().Equal(
            AchRecipientField.Name,
            AchRecipientField.Holder,
            AchRecipientField.Routing,
            AchRecipientField.Account,
            AchRecipientField.AccountType,
            AchRecipientField.Memo);
        errors.Select(static e => e.Message).Should().Equal(
            UserFacingStrings.AchEnterPayeeName,
            UserFacingStrings.AchEnterAccountHolderName,
            UserFacingStrings.AchRoutingNumberMustBeDigits(AchRecipientValidation.RoutingNumberDigitCount),
            UserFacingStrings.AchEnterValidAccountNumber,
            UserFacingStrings.AchAccountTypeMustBeCheckingOrSavings,
            UserFacingStrings.AchMemoMustBeMaxLength(AchRecipientValidation.MemoMaxLength));
    }

    [Fact]
    public void ValidateDetailed_ValidInput_ReturnsEmpty()
    {
        AchRecipientValidation.ValidateDetailed(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "021000021",
            "12345678",
            "checking",
            "April rent").Should().BeEmpty();
    }

    [Fact]
    public void ValidateAll_MatchesDetailedMessages()
    {
        IReadOnlyList<string> all = AchRecipientValidation.ValidateAll(
            " ",
            "Jane Doe",
            "Demo Bank",
            "02100",
            "12",
            "checking",
            null);

        IReadOnlyList<AchValidationError> detailed = AchRecipientValidation.ValidateDetailed(
            " ",
            "Jane Doe",
            "Demo Bank",
            "02100",
            "12",
            "checking",
            null);

        all.Should().Equal(detailed.Select(static e => e.Message));
    }

    [Fact]
    public void ValidateAll_ValidInput_ReturnsEmpty()
    {
        AchRecipientValidation.ValidateAll(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "021000021",
            "12345678",
            "checking",
            "April rent").Should().BeEmpty();
    }

    [Fact]
    public void Validate_FirstError_MatchesValidateAllHead()
    {
        string? first = AchRecipientValidation.Validate(
            " ",
            "Jane Doe",
            "Demo Bank",
            "02100",
            "12345678",
            "checking");

        IReadOnlyList<string> all = AchRecipientValidation.ValidateAll(
            " ",
            "Jane Doe",
            "Demo Bank",
            "02100",
            "12345678",
            "checking",
            null);

        first.Should().Be(all[0]);
    }

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
    public void Validate_RejectsUnicodeDigitRouting()
    {
        // Arabic-Indic characters are Unicode decimal digits but not the supported
        // ASCII 0-9 wire format for ABA routing numbers.
        AchRecipientValidation.Validate(
            "Rent LLC",
            "Jane Doe",
            "Demo Bank",
            "٠٢١٠٠٠٠٢١",
            "12345678",
            "checking").Should().Contain("9 digits");
    }

    [Fact]
    public void MaskRouting_IgnoresUnicodeDigits()
    {
        // Only ASCII digits count toward the visible trailing mask.
        AchRecipientValidation.MaskRouting("٠٢١٠٠٠٠٢١").Should().Be("••••");
    }

    [Fact]
    public void MaskAccount_KeepsLastFour() => AchRecipientValidation.MaskAccount("88210001").Should().Be("•••• 0001");
}
