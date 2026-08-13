// <copyright file="E2ETestsDefinition.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Xunit;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// xUnit collection so AccountStories / CoraShellSmokeTests share one Appium session via <see cref="AppiumFixtureHolder"/>.
/// Host-only harness Facts (e.g. E2EHarnessCredentialsTests) may share the collection to serialize env mutation.
/// Use: High (every E2E_RUN=1 collection). Scope: process-wide Appium session for the suite.
/// </summary>
[CollectionDefinition("E2E Tests")]
public sealed class E2ETestsDefinition : ICollectionFixture<AppiumFixtureHolder>
{
}
