// <copyright file="StreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>Stream event from /v1/stream.</summary>
public sealed class StreamEvent
{
    public string Type { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }
}

/// <summary>Product websocket / mock stream.</summary>
public interface IStreamService
{
    event EventHandler<StreamEvent>? EventReceived;

    Task ConnectAsync(CancellationToken ct = default);

    Task DisconnectAsync();

    bool IsConnected { get; }
}

/// <summary>Mock in-process stream that ticks rates periodically.</summary>
public sealed class MockStreamService : IStreamService, IAsyncDisposable
{
    // --- Tick cadence (only fires when subscribers exist) ---
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event EventHandler<StreamEvent>? EventReceived;

    public bool IsConnected => _loop is { IsCompleted: false };

    /// <summary>Test/helper: raise a stream event for hub wiring.</summary>
    public void Emit(StreamEvent e) => EventReceived?.Invoke(this, e);

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationToken token = _cts.Token;
        _loop = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                EventHandler<StreamEvent>? handlers = EventReceived;
                if (handlers is not null)
                {
                    handlers.Invoke(this, new StreamEvent { Type = "RATE.TICK" });
                    if (DateTimeOffset.UtcNow.Second % 2 == 0)
                    {
                        handlers.Invoke(this, new StreamEvent { Type = "balance.update" });
                    }
                }

                try
                {
                    await Task.Delay(TickInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
        return Task.CompletedTask;
    }

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
            catch
            {
                // ignored
            }
        }

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);
}

/// <summary>Live ClientWebSocket against EXPO_PUBLIC_WSS-style endpoint.</summary>
public sealed class ClientWebSocketStreamService : IStreamService, IAsyncDisposable
{
    // --- Receive buffer ---
    private const int ReceiveBufferBytes = 8 * 1024;

    private readonly Uri _uri;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public ClientWebSocketStreamService(string wssUrl)
    {
        _uri = new Uri(wssUrl);
    }

    public event EventHandler<StreamEvent>? EventReceived;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await DisconnectAsync().ConfigureAwait(false);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(_uri, _cts.Token).ConfigureAwait(false);
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    /// <summary>
    /// Accumulates WebSocket text fragments until EndOfMessage, then parses one JSON event.
    /// Use: High (while connected). Scope: ClientWebSocketStreamService receive loop.
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[ReceiveBufferBytes];
        using var message = new MemoryStream();
        while (_ws is { State: WebSocketState.Open } && !ct.IsCancellationRequested)
        {
            var result = await _ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                message.SetLength(0);
                continue;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            // Skip parse work when nobody is listening.
            if (EventReceived is null)
            {
                message.SetLength(0);
                continue;
            }

            string json = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);
            try
            {
                using var doc = JsonDocument.Parse(json);
                string type = doc.RootElement.TryGetProperty("TYPE", out var t)
                    ? t.GetString() ?? string.Empty
                    : doc.RootElement.TryGetProperty("type", out var t2) ? t2.GetString() ?? string.Empty : string.Empty;
                EventReceived?.Invoke(this, new StreamEvent { Type = type, Payload = doc.RootElement.Clone() });
            }
            catch
            {
                // ignore malformed
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            _cts.Dispose();
            _cts = null;
        }

        if (_ws is not null)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }

            _ws.Dispose();
            _ws = null;
        }
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);
}
