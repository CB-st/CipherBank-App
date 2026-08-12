// <copyright file="MockStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Mock in-process stream that ticks rates periodically.</summary>
public sealed class MockStreamService : IStreamService, IAsyncDisposable
{
    // --- Tick cadence (only fires when subscribers exist) ---
    private const int BalanceUpdateEveryNthSecond = 2;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public MockStreamService()
        : this(TimeProvider.System)
    {
    }

    public MockStreamService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public event EventHandler<StreamEventArgs>? EventReceived;

    public bool IsConnected => _loop is { IsCompleted: false };

    /// <summary>Test/helper: raise a stream event for hub wiring.</summary>
    public void Emit(StreamEventArgs e) => EventReceived?.Invoke(this, e);

    public async Task ConnectAsync(CancellationToken ct)
    {
        // Cancel any prior loop and await it so reconnect cannot orphan unobserved failures.
        await DisconnectAsync().ConfigureAwait(false);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationToken token = _cts.Token;
        _loop = Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    EventHandler<StreamEventArgs>? handlers = EventReceived;
                    if (handlers is not null)
                    {
                        handlers.Invoke(this, new StreamEventArgs { Type = "RATE.TICK" });
                        if (_timeProvider.GetUtcNow().Second % BalanceUpdateEveryNthSecond == 0)
                        {
                            handlers.Invoke(this, new StreamEventArgs { Type = "balance.update" });
                        }
                    }

                    try
                    {
                        await Task.Delay(TickInterval, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            },
            token);
    }

    /// <summary>
    /// Cancels the tick loop and awaits it, swallowing expected cancel/dispose faults.
    /// Use: Medium (disconnect / dispose). Scope: MockStreamService session.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }
            catch (AggregateException)
            {
                // ignored — loop may wrap cancel/dispose
            }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);
}
