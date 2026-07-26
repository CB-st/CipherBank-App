// <copyright file="EventDebouncer.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Coalesces rapid events into a single delayed action.</summary>
public sealed class EventDebouncer
{
    private readonly TimeSpan _delay;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private int _fireCount;

    public EventDebouncer(TimeSpan delay) => _delay = delay;

    public int FireCount => _fireCount;

    /// <summary>
    /// Schedules <paramref name="action"/> after the debounce delay, cancelling any prior pending fire.
    /// Use: High (stream bursts). Scope: this debouncer instance; CTS swap is serialized.
    /// </summary>
    public async Task DebounceAsync(Func<Task> action, CancellationToken outer)
    {
        ArgumentNullException.ThrowIfNull(action);
        CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(outer);
        CancellationTokenSource? prior;
        CancellationToken token;
        lock (_gate)
        {
            prior = _cts;
            _cts = linked;
            token = linked.Token;
        }

        if (prior is not null)
        {
            try
            {
                await prior.CancelAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Already torn down by a racing supersession.
            }

            prior.Dispose();
        }

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
        catch (ObjectDisposedException)
        {
            // Token source disposed by a newer DebounceAsync call.
        }
    }
}
