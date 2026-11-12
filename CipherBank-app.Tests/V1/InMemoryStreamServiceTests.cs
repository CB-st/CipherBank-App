// <copyright file="InMemoryStreamServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Concurrent;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class InMemoryStreamServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Emit_ForwardsExactEventOnlyToCurrentSubscribers(bool nullClock)
    {
        await using InMemoryStreamService stream = nullClock ? new(null!) : new();
        StreamEventArgs message = new() { Type = "custom.event" };
        List<(object? Sender, StreamEventArgs Event)> received = [];
        EventHandler<StreamEventArgs> handler = (sender, e) => received.Add((sender, e));

        stream.Emit(message);
        stream.EventReceived += handler;
        stream.Emit(message);
        stream.EventReceived -= handler;
        stream.Emit(message);
        await stream.DisconnectAsync();

        received.Should().ContainSingle().Which.Should().Be((stream, message));
        stream.IsConnected.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ConnectAsync_FirstTickUsesClockSecondForBalanceUpdate(int second)
    {
        await using InMemoryStreamService stream = new(new FixedTimeProvider(second));
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource tick = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<string> types = new();
        int expectedEvents = second == 0 ? 2 : 1;
        stream.EventReceived += (_, e) =>
        {
            types.Enqueue(e.Type);
            if (types.Count == expectedEvents)
            {
                cancellation.Cancel();
                tick.TrySetResult();
            }
        };

        await stream.ConnectAsync(cancellation.Token);
        await tick.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await stream.DisconnectAsync();

        types.Should().Equal(second == 0 ? ["RATE.TICK", "balance.update"] : ["RATE.TICK"]);
        stream.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WithoutSubscribers_ConnectsAndDisposes()
    {
        await using InMemoryStreamService stream = new(new FixedTimeProvider(1));
        stream.IsConnected.Should().BeFalse();

        await stream.ConnectAsync(CancellationToken.None);
        stream.IsConnected.Should().BeTrue();

        await stream.DisposeAsync();

        stream.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectAsync_ReconnectsWithOneFirstTickPerLoop_ThenDisconnects()
    {
        await using InMemoryStreamService stream = new(new FixedTimeProvider(1));
        ConcurrentQueue<string> types = new();
        for (int connection = 0; connection < 2; connection++)
        {
            using CancellationTokenSource cancellation = new();
            TaskCompletionSource tick = new(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<StreamEventArgs> handler = (_, e) =>
            {
                types.Enqueue(e.Type);
                cancellation.Cancel();
                tick.TrySetResult();
            };
            stream.EventReceived += handler;

            await stream.ConnectAsync(cancellation.Token);
            await tick.Task.WaitAsync(TimeSpan.FromSeconds(10));
            stream.EventReceived -= handler;
        }

        await stream.DisconnectAsync();

        types.Should().Equal("RATE.TICK", "RATE.TICK");
        stream.IsConnected.Should().BeFalse();
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("disposed")]
    [InlineData("aggregate")]
    public async Task DisconnectAsync_ObservesHandledSubscriberFailure_AndStopsLoop(string failure)
    {
        await using InMemoryStreamService stream = new(new FixedTimeProvider(1));
        TaskCompletionSource<string> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception error = failure switch
        {
            "cancel" => new OperationCanceledException(),
            "disposed" => new ObjectDisposedException("subscriber"),
            _ => new AggregateException(new InvalidOperationException("subscriber failed")),
        };
        stream.EventReceived += (_, e) =>
        {
            try
            {
                throw error;
            }
            finally
            {
                received.TrySetResult(e.Type);
            }
        };

        await stream.ConnectAsync(CancellationToken.None);
        (await received.Task.WaitAsync(TimeSpan.FromSeconds(10))).Should().Be("RATE.TICK");
        Func<Task> disconnect = () => stream.DisconnectAsync();

        await disconnect.Should().NotThrowAsync();
        stream.IsConnected.Should().BeFalse();
    }

    private sealed class FixedTimeProvider(int second) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddSeconds(second);
    }
}
