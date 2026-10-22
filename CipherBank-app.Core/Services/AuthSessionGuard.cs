// <copyright file="AuthSessionGuard.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Shares a logout generation across auth service instances so an in-flight
/// token refresh cannot write credentials after logout has cleared them.
/// </summary>
public sealed class AuthSessionGuard : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _generation;
    private bool _disposed;

    /// <summary>
    /// Captures the generation that a token write must still match.
    /// Call this before the refresh request, not after it returns.
    /// The read holds the storage gate, so it cannot observe a logout generation
    /// while that logout is still clearing credentials.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    /// <returns>The generation at the start of the refresh or login.</returns>
    public async Task<int> CaptureAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return Volatile.Read(ref _generation);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Persists tokens only when logout has not moved the generation since <paramref name="capturedGeneration"/>.
    /// </summary>
    /// <param name="capturedGeneration">The generation from <see cref="CaptureAsync"/>.</param>
    /// <param name="persist">Writes the tokens. It runs only when the generation still matches.</param>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    /// <returns>True when <paramref name="persist"/> ran.</returns>
    public async Task<bool> TryPersistAsync(
        int capturedGeneration,
        Func<Task> persist,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(persist);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (capturedGeneration != Volatile.Read(ref _generation))
            {
                return false;
            }

            await persist().ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Advances the generation and then clears stored credentials while holding the storage gate.
    /// A refresh that captured an older generation cannot persist after this returns,
    /// and a refresh already inside <see cref="TryPersistAsync"/> finishes before the clear.
    /// </summary>
    /// <param name="clear">Removes the stored credentials.</param>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    public async Task InvalidateAndClearAsync(Func<Task> clear, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clear);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Interlocked.Increment(ref _generation);
            await clear().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gate.Dispose();
        _disposed = true;
    }
}
