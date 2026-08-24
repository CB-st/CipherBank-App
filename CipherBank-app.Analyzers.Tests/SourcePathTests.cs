// <copyright file="SourcePathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class SourcePathTests
{
    [Fact]
    public void PathsEqual_IgnoresCase()
    {
        string path = Path.Combine("src", "Core", "File.cs");
        Assert.True(path.PathsEqual(path.ToUpperInvariant()));
    }

    [Fact]
    public void IsCSharpFile_UsesPathExtension()
    {
        Assert.True(Path.Combine("src", "Wallet.cs").IsCSharpFile());
        Assert.False(Path.Combine("src", "Wallet.csproj").IsCSharpFile());
    }

    [Fact]
    public void IsLegacyAssemblyInfo_UsesFileNameAndPropertiesFolder()
    {
        Assert.True(Path.Combine("CipherBank-app.Core", "Properties", "AssemblyInfo.cs").IsLegacyAssemblyInfo());
        Assert.True(Path.Combine("Properties", "AssemblyInfo.cs").IsLegacyAssemblyInfo());
        Assert.False(Path.Combine("Properties", "Other.cs").IsLegacyAssemblyInfo());
    }

    [Fact]
    public void IsCoreProject_FindsCoreDirectorySegment()
    {
        Assert.True(Path.Combine("src", "CipherBank-app.Core", "Persist", "LocalDb.cs").IsCoreProject());
        Assert.False(Path.Combine("src", "CipherBank-app", "MauiProgram.cs").IsCoreProject());
    }

    [Fact]
    public void IsSqlOwner_RequiresPersistSqlLocalDbSql()
    {
        Assert.True(Path.Combine("CipherBank-app.Core", "Persist", "Sql", "LocalDbSql.cs").IsSqlOwner());
        Assert.False(Path.Combine("CipherBank-app.Core", "Persist", "LocalDbSql.cs").IsSqlOwner());
        Assert.False(Path.Combine("CipherBank-app.Core", "Services", "Query.cs").IsSqlOwner());
    }

    [Fact]
    public void IsCentralPackageFile_UsesFileNameNotSuffix()
    {
        Assert.True(Path.Combine("src", "Directory.Packages.props").IsCentralPackageFile());
        Assert.True("Directory.Packages.props".IsCentralPackageFile());
        Assert.False("MyDirectory.Packages.props".IsCentralPackageFile());
    }

    [Fact]
    public void IsMsBuildProjectFile_UsesPathExtension()
    {
        Assert.True(Path.Combine("src", "App.csproj").IsMsBuildProjectFile());
        Assert.True(Path.Combine("src", "build.props").IsMsBuildProjectFile());
        Assert.True(Path.Combine("src", "build.targets").IsMsBuildProjectFile());
        Assert.False(Path.Combine("src", "App.cs").IsMsBuildProjectFile());
    }
}
