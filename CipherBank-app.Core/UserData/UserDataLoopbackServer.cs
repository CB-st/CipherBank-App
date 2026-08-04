// <copyright file="UserDataLoopbackServer.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>
/// Temporary localhost CIPHERBANK_INTERNAL self-server for unit/E2E cross-substantiation.
/// Speaks PlainJson frames; swap TcpUserDataTransport options to production when not testing.
/// </summary>
public sealed class UserDataLoopbackServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly UserDataServiceLogic _logic;
    private readonly PlainJsonUserDataWireCodec _codec;
    private readonly string _eof;
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;

    /// <summary>
    /// Binds 127.0.0.1:0 (ephemeral). Use: High (tests). Scope: process test harness.
    /// </summary>
    public UserDataLoopbackServer(UserDataServiceLogic logic, string eof = UserDataEndpointOptions.DefaultEof)
    {
        ArgumentNullException.ThrowIfNull(logic);
        _logic = logic;
        _codec = new PlainJsonUserDataWireCodec();
        _eof = eof;
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    /// <summary>Bound loopback port after <see cref="StartAsync"/>. Use: High (client options). Scope: tests.</summary>
    public int Port { get; private set; }

    /// <summary>
    /// Starts accepting connections. Use: High (tests). Scope: UserDataLoopbackServer.
    /// </summary>
    public Task StartAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = AcceptLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Endpoint options pointed at this self-server. Use: High (tests). Scope: harness.
    /// </summary>
    public UserDataEndpointOptions CreateClientOptions()
        => UserDataEndpointOptions.Loopback(Port, UserDataPayloadMode.PlainJson);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        _listener.Stop();
        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        _cts.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => HandleClientAsync(client, ct), ct);
        }
    }

    /// <summary>
    /// Serves one TCP client connection (one request frame).
    /// Use: High (accept loop). Scope: UserDataLoopbackServer.
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            await using NetworkStream stream = client.GetStream();
            string requestText = await UserDataTcpFrameIo.ReadUntilEofAsync(stream, _eof, ct).ConfigureAwait(false);
            UserDataApiFrame request = _codec.Decode(requestText);
            Dictionary<string, string> responsePayload = _logic.HandleRequest(request.MessageType, request.Payload);

            string responseType = responsePayload.TryGetValue("__MESSAGE_TYPE__", out string? mt)
                ? mt
                : UserDataWireNames.ErrorType;
            long code = responsePayload.TryGetValue("__CODE__", out string? codeText)
                && long.TryParse(codeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
                ? parsed
                : (long)UserDataStatusCode.UnknownRequest;
            string message = responsePayload.TryGetValue("__MESSAGE__", out string? msg) ? msg : string.Empty;

            string responseText = _codec.Encode(responseType, code, message, responsePayload) + _eof;
            byte[] bytes = Encoding.UTF8.GetBytes(responseText);
            await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // swallow per-connection errors; accept loop continues
        }
        finally
        {
            client.Dispose();
        }
    }
}
