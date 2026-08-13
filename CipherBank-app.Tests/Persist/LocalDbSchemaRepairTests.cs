// <copyright file="LocalDbSchemaRepairTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Data.Common;
using CipherBank_app.Persist;
using CipherBank_app.Persist.Sql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class LocalDbSchemaRepairTests
{
    /// <summary>
    /// Initialize must call the injected repair port instead of a static SQL bag.
    /// Use: Medium. Scope: LocalDb constructor injection.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_InvokesInjectedSchemaRepair()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-repair-" + Guid.NewGuid().ToString("N") + ".db");
        RecordingSchemaRepair repair = new RecordingSchemaRepair();
        LocalDb db = new LocalDb(path, repair);

        await db.InitializeAsync();

        repair.EnsureCalled.Should().BeTrue();
        repair.CompatibilityCalled.Should().BeTrue();
    }

    private sealed class RecordingSchemaRepair : ILegacySchemaRepair
    {
        public bool EnsureCalled { get; private set; }

        public bool CompatibilityCalled { get; private set; }

        public Task EnsureMissingModelTablesAsync(DbContext context, CancellationToken ct)
        {
            EnsureCalled = true;
            return Task.CompletedTask;
        }

        public Task ApplyCompatibilityAsync(DbConnection connection, CancellationToken ct)
        {
            CompatibilityCalled = true;
            return Task.CompletedTask;
        }
    }
}
