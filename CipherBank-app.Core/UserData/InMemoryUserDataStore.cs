// <copyright file="InMemoryUserDataStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Process-local userdata stash shared by MockUserDataClient and the loopback self-server.
/// </summary>
public sealed class InMemoryUserDataStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, UserDataIdentityRecord> _identities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _stashes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UserDataChallengeRecord> _challenges = new(StringComparer.Ordinal);

    /// <summary>
    /// Looks up identity by username hash. Use: High (all ops). Scope: in-memory store.
    /// </summary>
    public bool TryGetIdentity(string usernameHashHex, out UserDataIdentityRecord? identity)
    {
        lock (_gate)
        {
            return _identities.TryGetValue(usernameHashHex, out identity);
        }
    }

    /// <summary>
    /// Inserts identity; returns false if hash already present. Use: Medium (enroll). Scope: store.
    /// </summary>
    public bool TryAddIdentity(UserDataIdentityRecord identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        lock (_gate)
        {
            return _identities.TryAdd(identity.UsernameHashHex, identity);
        }
    }

    /// <summary>
    /// Reads opaque stash blob (may be empty). Use: High (grab). Scope: store.
    /// </summary>
    public string GetStash(string usernameHashHex)
    {
        lock (_gate)
        {
            return _stashes.TryGetValue(usernameHashHex, out string? blob) ? blob : string.Empty;
        }
    }

    /// <summary>
    /// Replaces stash and returns previous value. Use: High (overwrite). Scope: store.
    /// </summary>
    public string ReplaceStash(string usernameHashHex, string newBlobBase64)
    {
        ArgumentNullException.ThrowIfNull(newBlobBase64);
        lock (_gate)
        {
            string old = _stashes.TryGetValue(usernameHashHex, out string? blob) ? blob : string.Empty;
            _stashes[usernameHashHex] = newBlobBase64;
            return old;
        }
    }

    /// <summary>
    /// Stores the sole pending challenge for a user (clears prior). Use: High (challenge). Scope: store.
    /// </summary>
    public void SetChallenge(string usernameHashHex, UserDataChallengeRecord challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        lock (_gate)
        {
            _challenges[usernameHashHex] = challenge;
        }
    }

    /// <summary>
    /// Reads pending challenge if any. Use: High (grab/overwrite). Scope: store.
    /// </summary>
    public bool TryGetChallenge(string usernameHashHex, out UserDataChallengeRecord? challenge)
    {
        lock (_gate)
        {
            return _challenges.TryGetValue(usernameHashHex, out challenge);
        }
    }

    /// <summary>
    /// Clears pending challenges for a user. Use: High (after success). Scope: store.
    /// </summary>
    public void ClearChallenge(string usernameHashHex)
    {
        lock (_gate)
        {
            _challenges.Remove(usernameHashHex);
        }
    }
}
