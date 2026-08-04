// <copyright file="TcpUserDataTransport.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Sockets;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>
/// CIPHERBANK_INTERNAL TCP transport: connect to Host:Port, write frame+EOF, read until EOF.
/// Target is flexible via <see cref="UserDataEndpointOptions"/> (loopback vs production).
/// </summary>
public sealed class TcpUserDataTransport : IUserDataTransport
{
    private readonly UserDataEndpointOptions _options;
    private readonly IUserDataWireCodec _codec;

    /// <summary>
    /// Builds a TCP transport for the given endpoint. Use: Medium (DI / tests). Scope: userdata Core.
    /// </summary>
    public TcpUserDataTransport(UserDataEndpointOptions options, IUserDataWireCodec? codec = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _codec = codec ?? UserDataWireCodecFactory.Create(options.PayloadMode);
    }

    /// <inheritdoc />
    public async Task<UserDataApiFrame> ExchangeAsync(
        string requestType,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestType);
        ArgumentNullException.ThrowIfNull(payload);

        string requestText = _codec.Encode(requestType, code: 0, message: "OK", payload) + _options.EndOfFrame;
        byte[] requestBytes = Encoding.UTF8.GetBytes(requestText);

        using var client = new TcpClient();
        using CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(_options.ConnectTimeout);
        await client.ConnectAsync(_options.Host, _options.Port, connectCts.Token).ConfigureAwait(false);

        await using NetworkStream stream = client.GetStream();
        stream.ReadTimeout = (int)_options.IoTimeout.TotalMilliseconds;
        stream.WriteTimeout = (int)_options.IoTimeout.TotalMilliseconds;

        await stream.WriteAsync(requestBytes, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);

        string responseText = await UserDataTcpFrameIo.ReadUntilEofAsync(stream, _options.EndOfFrame, ct)
            .ConfigureAwait(false);
        return _codec.Decode(responseText);
    }
}
