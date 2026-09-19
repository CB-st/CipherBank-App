// <copyright file="UserFacingStringsResourceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Resources;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Resources;

public sealed class UserFacingStringsResourceTests
{
    [Fact]
    public void Ach_and_pin_messages_resolve_from_resource_manager()
    {
        UserFacingStrings.AchEnterPayeeName.Should().NotBeNullOrWhiteSpace();
        UserFacingStrings.AchRoutingNumberMustBeDigits(9).Should().Contain("9");
        UserFacingStrings.PinChangeTooShort(6).Should().Contain("6");
        UserFacingStrings.PinChangeSuccess.Should().Contain("PIN");
    }
}
