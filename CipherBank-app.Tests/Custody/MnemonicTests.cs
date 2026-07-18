// <copyright file="MnemonicTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class MnemonicTests
{
    [Fact]
    public void Generate_IsValidTwelveWords()
    {
        string phrase = MnemonicHelper.Generate();
        MnemonicHelper.Validate(phrase).Should().BeTrue();
        MnemonicHelper.Words(phrase).Should().HaveCount(12);
    }
}
