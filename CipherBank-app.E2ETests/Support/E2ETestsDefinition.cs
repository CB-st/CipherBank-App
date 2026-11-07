// <copyright file="E2ETestsDefinition.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// xUnit collection for E2E Facts. Full Appium fixture sharing lands on M4;
/// this M3 stub only defines the collection name so CriticalUserJourneyTests compiles.
/// Use: High (E2E collection attribute). Scope: E2ETests project compile gate.
/// </summary>
[CollectionDefinition("E2E Tests")]
public sealed class E2ETestsDefinition
{
}
