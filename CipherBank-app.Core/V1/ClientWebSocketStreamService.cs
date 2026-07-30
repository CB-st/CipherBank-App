// <copyright file="ClientWebSocketStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>Live ClientWebSocket against EXPO_PUBLIC_WSS-style endpoint.</summary>
public sealed class ClientWebSocketStreamService : IStreamService, IAsyncDisposable
{
    // --- Receive buffer ---
    private const int ReceiveBufferBytes = 8 * 1024;

    private readonly Uri _uri;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public ClientWebSocketStreamService(Uri wssUrl)
    {
        ArgumentNullException.ThrowIfNull(wssUrl);
        _uri = wssUrl;
    }

    public ClientWebSocketStreamService(string wssUrl)
        : this(new Uri(wssUrl))
    {
    }

    public event EventHandler<StreamEventArgs>? EventReceived;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken ct)
    {
        await DisconnectAsync().ConfigureAwait(false);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(_uri, _cts.Token).ConfigureAwait(false);
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
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

    private static bool TryParseStreamEvent(string json, out StreamEventArgs? streamEvent)
    {
        streamEvent = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var type = ExtractEventType(doc.RootElement);
            streamEvent = new StreamEventArgs { Type = type, Payload = doc.RootElement.Clone() };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractEventType(JsonElement root)
    {
        if (root.TryGetProperty("TYPE", out JsonElement upperType))
        {
            return upperType.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("type", out JsonElement lowerType))
        {
            return lowerType.GetString() ?? string.Empty;
        }

        return string.Empty;
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
            WebSocketReceiveResult result = await _ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                message.SetLength(0);
                continue;
            }

            await message.WriteAsync(buffer.AsMemory(0, result.Count), ct).ConfigureAwait(false);
            if (!result.EndOfMessage)
            {
                continue;
            }

            TryDispatchMessage(message);
        }
    }

    private void TryDispatchMessage(MemoryStream message)
    {
        if (EventReceived is null)
        {
            message.SetLength(0);
            return;
        }

        var json = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
        message.SetLength(0);
        if (!TryParseStreamEvent(json, out StreamEventArgs? streamEvent) || streamEvent is null)
        {
            return;
        }

        EventReceived?.Invoke(this, streamEvent);
    }
}
