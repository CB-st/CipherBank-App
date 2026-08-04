// <copyright file="UserDataPrefsSyncService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.V1;

namespace CipherBank_app.UserData;

/// <summary>
/// Pack-backed <see cref="IPrefsSyncService"/>: GRAB/OVERWRITE via <see cref="IUserDataClient"/>,
/// with optional dual-write to plaintext product prefs during migration.
/// </summary>
public sealed class UserDataPrefsSyncService : IPrefsSyncService
{
    private readonly IPrefsStore _store;
    private readonly IProductApi _productApi;
    private readonly IUserDataClient _userData;
    private readonly IUserDataAccountContext _account;
    private readonly IUserDataPackMetaStore _metaStore;
    private readonly IUserDataEnrollAlgorithm _enroll;
    private readonly UserDataPrefsSyncOptions _options;

    /// <summary>
    /// Builds pack-aware prefs sync. Use: Low (DI). Scope: userdata + V1 prefs.
    /// </summary>
    public UserDataPrefsSyncService(
        IPrefsStore store,
        IProductApi productApi,
        IUserDataClient userData,
        IUserDataAccountContext account,
        IUserDataPackMetaStore metaStore,
        UserDataPrefsSyncOptions? options = null,
        IUserDataEnrollAlgorithm? enroll = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(productApi);
        ArgumentNullException.ThrowIfNull(userData);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(metaStore);

        _store = store;
        _productApi = productApi;
        _userData = userData;
        _account = account;
        _metaStore = metaStore;
        _options = options ?? UserDataPrefsSyncOptions.DualWrite();
        _enroll = enroll ?? new RsaOaepSha256UserDataEnrollAlgorithm();
    }

    /// <inheritdoc />
    public async Task PullMergeAsync(CancellationToken ct)
    {
        UserPrefs local = await _store.LoadAsync().ConfigureAwait(false);
        if (_options.EnablePackSync && TryBeginUnlockedSession(out string username, out string mnemonic))
        {
            bool applied = await TryPullPackAsync(local, username, mnemonic, ct).ConfigureAwait(false);
            if (applied)
            {
                await _store.SaveAsync(local).ConfigureAwait(false);
                return;
            }
        }

        PrefsWireDto? remote = await _productApi.GetPrefsAsync(ct).ConfigureAwait(false);
        PrefsMerge.Merge(local, remote);
        await _store.SaveAsync(local).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        return SaveAndPushCoreAsync(prefs, ct);
    }

    /// <summary>
    /// Saves locally, OVERWRITEs userdata pack when unlocked, optionally dual-writes product prefs.
    /// Use: High (SaveAndPushAsync). Scope: this service.
    /// </summary>
    private async Task<bool> SaveAndPushCoreAsync(UserPrefs prefs, CancellationToken ct)
    {
        prefs.NormalizeHomeSections();
        await _store.SaveAsync(prefs).ConfigureAwait(false);

        bool packOk = false;
        if (_options.EnablePackSync && TryBeginUnlockedSession(out string username, out string mnemonic))
        {
            packOk = await TryPushPackAsync(prefs, username, mnemonic, ct).ConfigureAwait(false);
        }

        UserDataPackMeta meta = await _metaStore.LoadAsync(ct).ConfigureAwait(false);
        bool productOk = true;
        if (ShouldDualWriteProduct(packOk, meta))
        {
            productOk = await TryPutProductPrefsAsync(prefs, ct).ConfigureAwait(false);
        }

        return packOk || productOk;
    }

    /// <summary>
    /// GRAB + open prefs block into <paramref name="local"/>. Use: High (PullMerge). Scope: this service.
    /// </summary>
    private async Task<bool> TryPullPackAsync(
        UserPrefs local,
        string username,
        string mnemonic,
        CancellationToken ct)
    {
        try
        {
            using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(mnemonic);
            using UserDataEnrollKeyPair enrollKeys = _enroll.DeriveKeyPair(keys.EnrollSeed);
            await EnsureEnrolledAsync(username, enrollKeys.PublicKeyPem, ct).ConfigureAwait(false);

            UserDataChallengeIssue challenge = await _userData
                .ChallengeAsync(username, _account.Preferred2FaMethod, ct)
                .ConfigureAwait(false);
            if (!challenge.IsSuccess)
            {
                return false;
            }

            byte[] plain = _enroll.DecryptChallenge(challenge.EncryptedChallenge!, enrollKeys);
            try
            {
                UserDataGrabResult grab = await _userData.GrabAsync(username, plain, ct).ConfigureAwait(false);
                if (!grab.IsSuccess || string.IsNullOrWhiteSpace(grab.UserDataBlobBase64))
                {
                    return false;
                }

                UserDataPackWire pack = UserDataPackCodec.DecodeBlob(grab.UserDataBlobBase64);
                Dictionary<string, string> opened = UserDataPackCodec.OpenPack(pack, username, keys.Kek);
                if (!opened.TryGetValue(UserDataBlockTypes.Prefs, out string? prefsJson)
                    && !opened.TryGetValue("prefs", out prefsJson))
                {
                    return false;
                }

                UserDataPrefsPackMapper.ApplyPrefsBlockJson(local, prefsJson);
                await UpdateMetaFromRemotePackAsync(pack.ContentVersion, ct).ConfigureAwait(false);
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Seals local prefs into a pack and OVERWRITEs. Use: High (SaveAndPush). Scope: this service.
    /// </summary>
    private async Task<bool> TryPushPackAsync(
        UserPrefs prefs,
        string username,
        string mnemonic,
        CancellationToken ct)
    {
        try
        {
            using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(mnemonic);
            using UserDataEnrollKeyPair enrollKeys = _enroll.DeriveKeyPair(keys.EnrollSeed);
            await EnsureEnrolledAsync(username, enrollKeys.PublicKeyPem, ct).ConfigureAwait(false);

            UserDataPackMeta meta = await _metaStore.LoadAsync(ct).ConfigureAwait(false);
            uint nextVersion = meta.ContentVersion + 1;
            string prefsJson = UserDataPrefsPackMapper.ToPrefsBlockJson(prefs);
            UserDataPackWire pack = UserDataPackCodec.SealPack(
                username,
                nextVersion,
                keys.Kek,
                [new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, prefsJson)]);
            string blob = UserDataPackCodec.EncodeBlob(pack);

            UserDataChallengeIssue challenge = await _userData
                .ChallengeAsync(username, _account.Preferred2FaMethod, ct)
                .ConfigureAwait(false);
            if (!challenge.IsSuccess)
            {
                return false;
            }

            byte[] plain = _enroll.DecryptChallenge(challenge.EncryptedChallenge!, enrollKeys);
            try
            {
                UserDataOverwriteResult put = await _userData
                    .OverwriteAsync(username, plain, blob, ct)
                    .ConfigureAwait(false);
                if (!put.IsSuccess)
                {
                    return false;
                }

                meta.ContentVersion = nextVersion;
                meta.SuccessfulPackWrites++;
                await _metaStore.SaveAsync(meta, ct).ConfigureAwait(false);
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Soft-fail ENROLL (UsernameExists is success). Use: High. Scope: this service.
    /// </summary>
    private async Task EnsureEnrolledAsync(string username, string publicKeyPem, CancellationToken ct)
    {
        UserDataEnrollResult enroll = await _userData.EnrollAsync(username, publicKeyPem, ct).ConfigureAwait(false);
        if (!enroll.IsSuccess)
        {
            throw new InvalidOperationException($"Userdata enroll failed: {enroll.Code}");
        }
    }

    /// <summary>
    /// Syncs local meta content_version up to a remote pack version. Use: Medium. Scope: this service.
    /// </summary>
    private async Task UpdateMetaFromRemotePackAsync(uint remoteVersion, CancellationToken ct)
    {
        UserDataPackMeta meta = await _metaStore.LoadAsync(ct).ConfigureAwait(false);
        if (remoteVersion > meta.ContentVersion)
        {
            meta.ContentVersion = remoteVersion;
            await _metaStore.SaveAsync(meta, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Dual-write product prefs when migration window says so. Use: High. Scope: this service.
    /// </summary>
    private bool ShouldDualWriteProduct(bool packOk, UserDataPackMeta meta)
    {
        if (!_options.DualWriteProductPrefs)
        {
            return false;
        }

        if (!_options.EnablePackSync || !packOk)
        {
            return true;
        }

        int limit = _options.DisableProductPushAfterSuccessfulPackWrites;
        return limit <= 0 || meta.SuccessfulPackWrites <= limit;
    }

    private async Task<bool> TryPutProductPrefsAsync(UserPrefs prefs, CancellationToken ct)
    {
        try
        {
            await _productApi.PutPrefsAsync(PrefsWireDto.FromUserPrefs(prefs), ct).ConfigureAwait(false);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves username + unlocked mnemonic for pack crypto. Use: High. Scope: this service.
    /// </summary>
    private bool TryBeginUnlockedSession(out string username, out string mnemonic)
    {
        username = string.Empty;
        mnemonic = string.Empty;
        if (string.IsNullOrWhiteSpace(_account.Username))
        {
            return false;
        }

        if (!_account.TryGetUnlockedMnemonic(out mnemonic) || string.IsNullOrWhiteSpace(mnemonic))
        {
            return false;
        }

        username = _account.Username;
        return true;
    }
}
