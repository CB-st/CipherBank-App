// <copyright file="H1H2ThreeKeyStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.V1;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

/// <summary>
/// Models M7 ProductSessionStorage blob 1f627c1: three sequential asynchronous writes/reads
/// and synchronous removal. Deliberately contains no lifecycle policy or cancellation API.
/// Save pauses after the first key; read pauses after collecting the three-key tuple.
/// Each key access itself is thread safe.
/// </summary>
internal sealed class H1H2ThreeKeyStore : IProductSessionStore
{
    private readonly Dictionary<string, string> _keys = [];
    private readonly object _gate = new();
    private int _saves;
    private int _clears;

    internal int SaveCalls => Volatile.Read(ref _saves);

    internal int ClearCalls => Volatile.Read(ref _clears);

    internal AsyncGate? ClearGate { get; set; }

    internal AsyncGate? SaveGate { get; set; }

    internal AsyncGate? ReadGate { get; set; }

    internal AsyncGate? ReadFirstKeyGate { get; set; }

    internal bool FailAfterFirstWrite { get; set; }

    internal int KeyCount
    {
        get
        {
            lock (_gate)
            {
                return _keys.Count;
            }
        }
    }

    public async Task SaveAsync(SessionDto session)
    {
        Interlocked.Increment(ref _saves);
        Set("access", session.AccessToken);
        if (SaveGate is { } gate)
        {
            await gate.PauseAsync(CancellationToken.None);
        }

        if (FailAfterFirstWrite)
        {
            throw new InvalidOperationException("secure write failed after access key");
        }

        Set("refresh", session.RefreshToken);
        Set("expires", DateTimeOffset.FromUnixTimeMilliseconds(session.ExpiresAt).ToString("O", CultureInfo.InvariantCulture));
    }

    public async Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync()
    {
        string? access = Get("access");
        if (ReadFirstKeyGate is { } firstKey)
        {
            await firstKey.PauseAsync(CancellationToken.None);
        }

        string? refresh = Get("refresh");
        string? expires = Get("expires");
        if (ReadGate is { } gate)
        {
            await gate.PauseAsync(CancellationToken.None);
        }

        return string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)
            || !DateTimeOffset.TryParse(expires, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsed)
            ? null : (access, refresh, parsed);
    }

    public void Clear()
    {
        Interlocked.Increment(ref _clears);
        // The legacy boundary is synchronous. Pause before removing keys, outside the key lock.
        ClearGate?.PauseAsync().GetAwaiter().GetResult();
        lock (_gate)
        {
            _keys.Remove("access");
            _keys.Remove("refresh");
            _keys.Remove("expires");
        }
    }

    private void Set(string key, string value)
    {
        lock (_gate)
        {
            _keys[key] = value;
        }
    }

    private string? Get(string key)
    {
        lock (_gate)
        {
            return _keys.GetValueOrDefault(key);
        }
    }
}
