// <copyright file="AppiumFixtureHolder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Owns the optional AppiumFixture for the E2E Tests collection (null when E2E_RUN is unset).
/// Use: High (collection construction). Scope: one Appium session per test collection.
/// </summary>
public sealed class AppiumFixtureHolder : IDisposable
{
    /// <summary>
    /// Builds the shared session once for the collection via <see cref="AppiumFixture.CreateOrThrow"/>.
    /// Use: High (collection fixture setup). Scope: E2E Tests collection.
    /// </summary>
    public AppiumFixtureHolder()
    {
        Fixture = AppiumFixture.CreateOrThrow();
    }

    /// <summary>Gets fixture for AppiumFixtureHolder.</summary>
    public AppiumFixture? Fixture { get; }

    /// <summary>Disposes the shared Appium session at collection teardown. Use: High. Scope: this holder.</summary>
    public void Dispose()
    {
        Fixture?.Dispose();
        GC.SuppressFinalize(this);
    }
}
