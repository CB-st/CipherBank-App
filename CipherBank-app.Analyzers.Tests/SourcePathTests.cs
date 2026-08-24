// <copyright file="SourcePathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class SourcePathTests
{
    [Fact]
    public void From_UsesPathFileNameAndExtension()
    {
        string path = Path.Combine("a", "b", "c.cs");
        Assert.Equal("c.cs", SourcePath.From(path).FileName);
        Assert.Equal(".cs", SourcePath.From(path).Extension);
    }

    [Fact]
    public void Equals_IgnoresCase()
    {
        string path = Path.Combine("src", "Core", "File.cs");
        Assert.True(SourcePath.From(path).Equals(SourcePath.From(path.ToUpperInvariant())));
    }

    [Fact]
    public void IsCSharpFile_UsesPathExtension()
    {
        Assert.True(SourcePath.From(Path.Combine("src", "Wallet.cs")).IsCSharpFile);
        Assert.False(SourcePath.From(Path.Combine("src", "Wallet.csproj")).IsCSharpFile);
    }

    [Fact]
    public void IsLegacyAssemblyInfo_UsesFileNameAndPropertiesFolder()
    {
        Assert.True(SourcePath.From(Path.Combine("CipherBank-app.Core", "Properties", "AssemblyInfo.cs")).IsLegacyAssemblyInfo);
        Assert.True(SourcePath.From(Path.Combine("Properties", "AssemblyInfo.cs")).IsLegacyAssemblyInfo);
        Assert.False(SourcePath.From(Path.Combine("Properties", "Other.cs")).IsLegacyAssemblyInfo);
    }

    [Fact]
    public void IsCoreProject_FindsCoreDirectorySegment()
    {
        Assert.True(SourcePath.From(Path.Combine("src", "CipherBank-app.Core", "Persist", "LocalDb.cs")).IsCoreProject);
        Assert.False(SourcePath.From(Path.Combine("src", "CipherBank-app", "MauiProgram.cs")).IsCoreProject);
    }

    [Fact]
    public void IsSqlOwner_RequiresPersistSqlLocalDbSql()
    {
        Assert.True(SourcePath.From(Path.Combine("CipherBank-app.Core", "Persist", "Sql", "LocalDbSql.cs")).IsSqlOwner);
        Assert.False(SourcePath.From(Path.Combine("CipherBank-app.Core", "Persist", "LocalDbSql.cs")).IsSqlOwner);
        Assert.False(SourcePath.From(Path.Combine("CipherBank-app.Core", "Services", "Query.cs")).IsSqlOwner);
    }

    [Fact]
    public void IsCentralPackageFile_UsesFileNameNotSuffix()
    {
        Assert.True(SourcePath.From(Path.Combine("src", "Directory.Packages.props")).IsCentralPackageFile);
        Assert.True(SourcePath.From("Directory.Packages.props").IsCentralPackageFile);
        Assert.False(SourcePath.From("MyDirectory.Packages.props").IsCentralPackageFile);
    }

    [Fact]
    public void IsMsBuildProjectFile_UsesPathExtension()
    {
        Assert.True(SourcePath.From(Path.Combine("src", "App.csproj")).IsMsBuildProjectFile);
        Assert.True(SourcePath.From(Path.Combine("src", "build.props")).IsMsBuildProjectFile);
        Assert.True(SourcePath.From(Path.Combine("src", "build.targets")).IsMsBuildProjectFile);
        Assert.False(SourcePath.From(Path.Combine("src", "App.cs")).IsMsBuildProjectFile);
    }
}
