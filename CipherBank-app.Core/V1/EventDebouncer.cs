// <copyright file="EventDebouncer.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Coalesces rapid events into a single delayed action.</summary>
public sealed class EventDebouncer
{
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _cts;
    private int _fireCount;

    public EventDebouncer(TimeSpan delay) => _delay = delay;

    public int FireCount => _fireCount;

    public async Task DebounceAsync(Func<Task> action, CancellationToken outer = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
        CancellationToken token = _cts.Token;
        try
        {
            await Task.Delay(_delay, token).ConfigureAwait(false);
            await action().ConfigureAwait(false);
            Interlocked.Increment(ref _fireCount);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer event.
        }
    }
}
