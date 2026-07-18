// <copyright file="StreamHub.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Process-wide fan-out of product stream events (one subscription to the socket).</summary>
public interface IStreamHub
{
    event EventHandler<StreamEvent>? EventReceived;

    bool IsRunning { get; }

    void Start();

    void Stop();
}

/// <inheritdoc />
public sealed class StreamHub : IStreamHub
{
    private readonly IStreamService _stream;
    private readonly object _gate = new();
    private bool _hooked;

    public StreamHub(IStreamService stream) => _stream = stream;

    public event EventHandler<StreamEvent>? EventReceived;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        lock (_gate)
        {
            if (_hooked)
            {
                return;
            }

            _stream.EventReceived += OnStreamEvent;
            _hooked = true;
            IsRunning = true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_hooked)
            {
                return;
            }

            _stream.EventReceived -= OnStreamEvent;
            _hooked = false;
            IsRunning = false;
        }
    }

    private void OnStreamEvent(object? sender, StreamEvent e)
        => EventReceived?.Invoke(this, e);
}
