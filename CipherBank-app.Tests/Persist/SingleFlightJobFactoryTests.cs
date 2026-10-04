// <copyright file="SingleFlightJobFactoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class SingleFlightJobFactoryTests
{
    [Fact]
    public async Task GetOrCreateAsync_DuplicateKeyReturnsSameTaskAndRunsOnce()
    {
        SingleFlightJobFactory factory = new();
        PersistOhlcJobKey key = new("BTC");
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int runs = 0;

        Task first = factory.GetOrCreateAsync(key, async () =>
        {
            Interlocked.Increment(ref runs);
            await release.Task;
        });
        Task duplicate = factory.GetOrCreateAsync(
            new PersistOhlcJobKey(" btc "),
            () =>
            {
                Interlocked.Increment(ref runs);
                return Task.CompletedTask;
            });

        duplicate.Should().BeSameAs(first);
        Volatile.Read(ref runs).Should().Be(1);
        release.SetResult();
        await first;
        Volatile.Read(ref runs).Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_TerminalTaskAllowsExactReentry()
    {
        SingleFlightJobFactory factory = new();
        RefreshRatesJobKey key = new();
        Task failed = factory.GetOrCreateAsync(
            key,
            () => throw new NotSupportedException("quote failed"));

        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<NotSupportedException>();

        Task retry = factory.GetOrCreateAsync(key, () => Task.CompletedTask);
        retry.Should().NotBeSameAs(failed);
        await retry;
    }
}
