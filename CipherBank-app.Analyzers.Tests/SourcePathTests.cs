// <copyright file="SourcePathTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class SourcePathTests
{
    [Fact]
    public void NormalizeSlashes_UnifiesBackslashes()
    {
        Assert.Equal("a/b/c.cs", @"a\b\c.cs".NormalizeSlashes());
    }

    [Fact]
    public void PathsEqual_IgnoresSlashStyleAndCase()
    {
        Assert.True(@"src\Core\File.cs".PathsEqual("src/core/file.cs"));
    }

    [Fact]
    public void IsCSharpFile_UsesPathExtension()
    {
        Assert.True(@"src\Wallet.cs".IsCSharpFile());
        Assert.False(@"src\Wallet.csproj".IsCSharpFile());
    }

    [Fact]
    public void IsLegacyAssemblyInfo_UsesFileNameAndPropertiesFolder()
    {
        Assert.True(@"CipherBank-app.Core\Properties\AssemblyInfo.cs".IsLegacyAssemblyInfo());
        Assert.True("Properties/AssemblyInfo.cs".IsLegacyAssemblyInfo());
        Assert.False(@"Properties\Other.cs".IsLegacyAssemblyInfo());
    }

    [Fact]
    public void IsCoreProject_FindsCoreSegmentOnWindowsPaths()
    {
        Assert.True(@"C:\src\CipherBank-app.Core\Persist\LocalDb.cs".IsCoreProject());
        Assert.False(@"C:\src\CipherBank-app\MauiProgram.cs".IsCoreProject());
    }

    [Fact]
    public void IsCentralPackageFile_UsesFileNameNotSuffix()
    {
        Assert.True(@"C:\src\Directory.Packages.props".IsCentralPackageFile());
        Assert.True("Directory.Packages.props".IsCentralPackageFile());
        Assert.False("MyDirectory.Packages.props".IsCentralPackageFile());
    }

    [Fact]
    public void IsMsBuildProjectFile_UsesPathExtension()
    {
        Assert.True(@"src\App.csproj".IsMsBuildProjectFile());
        Assert.True(@"src\build.props".IsMsBuildProjectFile());
        Assert.True(@"src\build.targets".IsMsBuildProjectFile());
        Assert.False(@"src\App.cs".IsMsBuildProjectFile());
    }
}
