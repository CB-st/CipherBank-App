// <copyright file="StringsResourceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Resources;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Resources;

public sealed class StringsResourceTests
{
    [Fact]
    public void Ach_and_pin_messages_resolve_from_resource_manager()
    {
        Strings.AchEnterPayeeName.Should().NotBeNullOrWhiteSpace();
        Strings.AchRoutingNumberMustBeDigits(9).Should().Contain("9");
        Strings.PinChangeTooShort(6).Should().Contain("6");
        Strings.PinChangeSuccess.Should().Contain("PIN");
    }
}
