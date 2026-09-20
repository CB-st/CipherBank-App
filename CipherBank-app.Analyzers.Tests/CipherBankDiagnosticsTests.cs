// <copyright file="CipherBankDiagnosticsTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class CipherBankDiagnosticsTests
{
    [Fact]
    public void RuleIds_AreStable()
    {
        // TODO: Build a way to generate tests from the descriptors, matrix of inputs to tests.
        Assert.Equal("CB1001", CipherBankDiagnostics.CentralPackageVersion.Id);
        Assert.Equal("CB1002", CipherBankDiagnostics.LegacyAssemblyInfo.Id);
        Assert.Equal("CB1003", CipherBankDiagnostics.ScatteredSql.Id);
        Assert.Equal("CB1004", CipherBankDiagnostics.RetiredApiName.Id);
        Assert.Equal("CipherBank.Structure", CipherBankDiagnostics.Category);
        Assert.Equal("CipherBank.Structure", CipherBankDiagnostics.CentralPackageVersion.Category);
        Assert.Equal("CipherBank.Structure", CipherBankDiagnostics.LegacyAssemblyInfo.Category);
        Assert.Equal("CipherBank.Structure", CipherBankDiagnostics.ScatteredSql.Category);
        Assert.Equal("CipherBank.Structure", CipherBankDiagnostics.RetiredApiName.Category);
    }
}
