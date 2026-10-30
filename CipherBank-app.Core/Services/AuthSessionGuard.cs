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
    /// <returns>The generation at the start of the refresh or login.</returns>
    public Task<int> CaptureAsync() => CaptureAsync(CancellationToken.None);

    /// <summary>Captures the generation while honoring cancellation while waiting for the storage gate.</summary>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    public Task<int> CaptureAsync(CancellationToken cancellationToken) => CaptureCoreAsync(cancellationToken);

    /// <summary>Captures the generation before reading and validating the supplied refresh token.</summary>
    public Task<int> CaptureRefreshAsync(string refreshToken, Func<Task<string?>> readRefreshToken)
        => CaptureRefreshAsync(refreshToken, readRefreshToken, CancellationToken.None);

    /// <summary>Captures and validates a refresh token while honoring cancellation for the gate wait.</summary>
    public Task<int> CaptureRefreshAsync(
        string refreshToken,
        Func<Task<string?>> readRefreshToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readRefreshToken);
        return CaptureRefreshCoreAsync(refreshToken, readRefreshToken, cancellationToken);
    }

    /// <summary>Reads a logout token under the same gate as its session generation.</summary>
    public Task<(int Generation, string? RefreshToken)?> CaptureLogoutAsync(Func<Task<string?>> readRefreshToken)
        => CaptureLogoutAsync(readRefreshToken, null);

    /// <summary>Reads a logout token only when the supplied session is still current.</summary>
    public Task<(int Generation, string? RefreshToken)?> CaptureLogoutAsync(
        Func<Task<string?>> readRefreshToken,
        int? capturedGeneration)
    {
        ArgumentNullException.ThrowIfNull(readRefreshToken);
        return CaptureLogoutCoreAsync(readRefreshToken, capturedGeneration);
    }

    /// <summary>Persists tokens only when the generation still matches.</summary>
    public Task<bool> TryPersistAsync(int capturedGeneration, Func<Task> persist)
        => TryPersistAsync(capturedGeneration, persist, false, CancellationToken.None);

    /// <summary>Persists tokens while honoring cancellation for the gate wait.</summary>
    public Task<bool> TryPersistAsync(int capturedGeneration, Func<Task> persist, CancellationToken cancellationToken)
        => TryPersistAsync(capturedGeneration, persist, false, cancellationToken);

    /// <summary>Persists tokens and optionally advances the generation for a new session.</summary>
    public Task<bool> TryPersistAsync(int capturedGeneration, Func<Task> persist, bool startsSession)
        => TryPersistAsync(capturedGeneration, persist, startsSession, CancellationToken.None);

    /// <summary>
    /// Persists tokens only when logout has not moved the generation since <paramref name="capturedGeneration"/>.
    /// </summary>
    /// <param name="capturedGeneration">The generation from <see cref="CaptureAsync()"/>.</param>
    /// <param name="persist">Writes the tokens. It runs only when the generation still matches.</param>
    /// <param name="startsSession">Advance the generation atomically with a successful login write.</param>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    /// <returns>True when <paramref name="persist"/> ran.</returns>
    public Task<bool> TryPersistAsync(
        int capturedGeneration,
        Func<Task> persist,
        bool startsSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(persist);
        return TryPersistCoreAsync(capturedGeneration, persist, startsSession, cancellationToken);
    }

    /// <summary>Advances the generation and clears stored credentials while holding the storage gate.</summary>
    public Task<bool> InvalidateAndClearAsync(Func<Task> clear)
        => InvalidateAndClearAsync(clear, null, CancellationToken.None);

    /// <summary>Invalidates and clears while honoring cancellation for the storage gate wait.</summary>
    public Task<bool> InvalidateAndClearAsync(Func<Task> clear, CancellationToken cancellationToken)
        => InvalidateAndClearAsync(clear, null, cancellationToken);

    /// <summary>Invalidates and clears only the supplied session.</summary>
    public Task<bool> InvalidateAndClearAsync(Func<Task> clear, int? capturedGeneration)
        => InvalidateAndClearAsync(clear, capturedGeneration, CancellationToken.None);

    /// <summary>
    /// Advances the generation and then clears stored credentials while holding the storage gate.
    /// A refresh that captured an older generation cannot persist after this returns,
    /// and a refresh already inside <see cref="TryPersistAsync(int, Func{Task})"/> finishes before the clear.
    /// </summary>
    /// <param name="clear">Removes the stored credentials.</param>
    /// <param name="capturedGeneration">When supplied, clear only this session.</param>
    /// <param name="cancellationToken">Cancels the wait for the storage gate.</param>
    /// <returns>True when this session was invalidated and cleared.</returns>
    public Task<bool> InvalidateAndClearAsync(
        Func<Task> clear,
        int? capturedGeneration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clear);
        return InvalidateAndClearCoreAsync(clear, capturedGeneration, cancellationToken);
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

    private async Task<int> CaptureCoreAsync(CancellationToken cancellationToken)
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

    private async Task<int> CaptureRefreshCoreAsync(
        string refreshToken,
        Func<Task<string?>> readRefreshToken,
        CancellationToken cancellationToken)
    {
        int generation = await CaptureAsync(cancellationToken).ConfigureAwait(false);
        string? stored = await readRefreshToken().ConfigureAwait(false);
        if (string.IsNullOrEmpty(stored) || !string.Equals(stored, refreshToken, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        return generation;
    }

    private async Task<(int Generation, string? RefreshToken)?> CaptureLogoutCoreAsync(
        Func<Task<string?>> readRefreshToken,
        int? capturedGeneration)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (capturedGeneration.HasValue && capturedGeneration.Value != _generation)
            {
                return null;
            }

            return (_generation, await readRefreshToken().ConfigureAwait(false));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> TryPersistCoreAsync(
        int capturedGeneration,
        Func<Task> persist,
        bool startsSession,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (capturedGeneration != Volatile.Read(ref _generation))
            {
                return false;
            }

            if (startsSession)
            {
                Interlocked.Increment(ref _generation);
            }

            await persist().ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> InvalidateAndClearCoreAsync(
        Func<Task> clear,
        int? capturedGeneration,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (capturedGeneration.HasValue && capturedGeneration.Value != _generation)
            {
                return false;
            }

            Interlocked.Increment(ref _generation);
            await clear().ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
