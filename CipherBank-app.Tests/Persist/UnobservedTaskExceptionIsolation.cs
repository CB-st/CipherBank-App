// <copyright file="UnobservedTaskExceptionIsolation.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Xunit;

namespace CipherBank_app.Tests.Persist;

/// <summary>
/// Tests that subscribe to the process-wide <see cref="TaskScheduler.UnobservedTaskException"/>.
/// xUnit runs this collection by itself, after the parallel collections, so no other test runs
/// while the handler is attached.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class UnobservedTaskExceptionIsolation
{
    /// <summary>Collection name used by <see cref="CollectionAttribute"/>.</summary>
    public const string Name = "UnobservedTaskException";
}
