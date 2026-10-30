// <copyright file="AppStartupCoordinatorTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class AppStartupCoordinatorTests
{
    [Fact]
    public async Task InitializeAsync_OrdersDatabaseBeforeRecipientsAndRunsOnce()
    {
        List<string> order = [];
        RecordingDatabaseInitializer database = new(order);
        RecordingRecipientInitializer recipients = new(order);
        AppStartupCoordinator coordinator = new(database, recipients);

        await Task.WhenAll(
            coordinator.InitializeAsync(CancellationToken.None),
            coordinator.InitializeAsync(CancellationToken.None));

        order.Should().Equal("database", "recipients");
        database.Calls.Should().Be(1);
        recipients.Calls.Should().Be(1);
    }

    private sealed class RecordingDatabaseInitializer(List<string> order)
        : ILocalDatabaseInitializer
    {
        internal int Calls { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            Calls++;
            order.Add("database");
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRecipientInitializer(List<string> order)
        : IRecipientSeedInitializer
    {
        internal int Calls { get; private set; }

        public Task InitializeAsync(CancellationToken ct)
        {
            Calls++;
            order.Add("recipients");
            return Task.CompletedTask;
        }
    }
}
