// <copyright file="PinAndCustodyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class PinAndCustodyTests
{
    [Theory]
    [InlineData("223456")]
    [InlineData("123556")]
    [InlineData("123455")]
    public async Task Custody_UnlockRejectsPinMismatchAtAnyPosition(string attemptedPin)
    {
        MemStore store = new();
        PinService pin = new(store);
        CustodyService custody = new(store, pin);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        await custody.SealAsync(mnemonic, "123456");
        custody.Lock();

        (await custody.UnlockAsync(attemptedPin)).Should().BeFalse();
        custody.IsUnlocked.Should().BeFalse();
        custody.ExportMnemonic().Should().BeNull();
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
    }

    [Fact]
    public async Task Pin_VerifySucceedsAfterSet()
    {
        PinService pin = new PinService(new MemStore());
        await pin.SetPinAsync("654321");
        (await pin.VerifyPinAsync("654321")).Should().BeTrue();
        (await pin.VerifyPinAsync("000000")).Should().BeFalse();
    }

    [Fact]
    public async Task Pin_SetPinAsync_RejectsShortPin()
    {
        PinService pin = new PinService(new MemStore());
        Func<Task> act = () => pin.SetPinAsync("12345");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Pin_Verify_RecoversStagedSaltHashPair()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        await pin.SetPinAsync("654321");
        string? oldHash = await store.GetAsync("cb_pin_hash");

        await pin.SetPinAsync("111111");
        string? newSalt = await store.GetAsync("cb_pin_salt");
        string? newHash = await store.GetAsync("cb_pin_hash");

        // Torn promote: staged pair complete, promoted hash still previous PIN's hash.
        await store.SetAsync("cb_pin_salt_staging", newSalt!);
        await store.SetAsync("cb_pin_hash_staging", newHash!);
        await store.SetAsync("cb_pin_salt", newSalt!);
        await store.SetAsync("cb_pin_hash", oldHash!);

        (await pin.VerifyPinAsync("111111")).Should().BeTrue();
        (await store.GetAsync("cb_pin_hash")).Should().Be(newHash);
        (await store.GetAsync("cb_pin_salt_staging")).Should().BeNull();
    }

    [Fact]
    public async Task Custody_Unlock_ClearsSessionOnFailedPin()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        (await custody.UnlockAsync("000000")).Should().BeFalse();
        custody.IsUnlocked.Should().BeFalse();
        custody.ExportMnemonic().Should().BeNull();
    }

    [Fact]
    public async Task Custody_Unlock_FailedPin_RaisesLocked()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        int locked = 0;
        custody.Locked += (_, _) => locked++;
        (await custody.UnlockAsync("000000")).Should().BeFalse();
        locked.Should().Be(1);
        custody.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Custody_IsUnlocked_DoesNotLockWhenSessionExpires()
    {
        ManualTime time = new ManualTime();
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin, time);
        await custody.SealAsync(MnemonicHelper.Generate(), "123456");
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        int locked = 0;
        custody.Locked += (_, _) => locked++;

        time.UtcNow = time.UtcNow.AddMinutes(6);

        custody.IsUnlocked.Should().BeFalse();
        locked.Should().Be(0);
        custody.ExportMnemonic().Should().BeNull();
        locked.Should().Be(1);
        custody.ExportMnemonic().Should().BeNull();
        locked.Should().Be(1);
    }

    [Fact]
    public async Task Custody_Unlock_MissingBlob_ClearsExistingSessionAndRaisesLocked()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        await custody.SealAsync(MnemonicHelper.Generate(), "123456");
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        int locked = 0;
        custody.Locked += (_, _) => locked++;
        await store.RemoveAsync(CustodyService.BlobKey);

        (await custody.UnlockAsync("123456")).Should().BeFalse();

        custody.IsUnlocked.Should().BeFalse();
        custody.ExportMnemonic().Should().BeNull();
        locked.Should().Be(1);
    }

    [Fact]
    public async Task Custody_SealAsync_RejectsShortPin()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        Func<Task> act = () => custody.SealAsync(MnemonicHelper.Generate(), "12");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void AesGcmCryptoBox_Open_LegacyBlobWithVersionLikeSaltPrefix()
    {
        CryptographyOptions options = CryptographyOptions.Default;
        AesGcmCryptoBox box = new AesGcmCryptoBox(options);
        const string pin = "123456";
        const string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        // Build a legacy-layout blob whose first salt byte is 0x01 (version marker collision).
        byte[] salt = RandomNumberGenerator.GetBytes(options.SaltSizeBytes);
        salt[0] = 0x01;
        using SensitiveBuffer keyBuffer = box.DeriveKey(pin, salt);
        byte[] key = keyBuffer.Span.ToArray();
        byte[] nonce = RandomNumberGenerator.GetBytes(options.NonceSizeBytes);
        byte[] plain = Encoding.UTF8.GetBytes(mnemonic);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[options.TagSizeBytes];
        using (AesGcm aes = new AesGcm(key, options.TagSizeBytes))
        {
            aes.Encrypt(nonce, plain, cipher, tag);
        }

        byte[] packed = new byte[salt.Length + nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(salt, 0, packed, 0, salt.Length);
        Buffer.BlockCopy(nonce, 0, packed, salt.Length, nonce.Length);
        Buffer.BlockCopy(tag, 0, packed, salt.Length + nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, packed, salt.Length + nonce.Length + tag.Length, cipher.Length);

        box.Open(Convert.ToBase64String(packed), pin).Should().Be(mnemonic);
        box.Open(box.Seal(mnemonic, pin), pin).Should().Be(mnemonic);
    }

    [Fact]
    public async Task Custody_SealUnlockRoundTrip()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        custody.Lock();
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(MnemonicHelper.Normalize(mnemonic));
    }

    [Fact]
    public async Task Custody_DeviceSecretUnlock_RecoversInterruptedReseal()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Normalize(MnemonicHelper.Generate());
        await custody.SealAsync(mnemonic, "123456");
        custody.Lock();

        string? stalePromoted = await store.GetAsync(CustodyService.DeviceSecretKey);
        stalePromoted.Should().NotBeNullOrEmpty();

        // Interrupted PersistDeviceSecretSealAsync after blob rewrite: new seal + staged secret,
        // old promoted secret still present.
        string stagedSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await store.SetAsync(CustodyService.StagingDeviceSecretKey, stagedSecret);
        await store.SetAsync(
            CustodyService.BlobKey,
            CryptoBox.Seal(CryptographyOptions.Default, mnemonic, stagedSecret));

        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
        (await store.GetAsync(CustodyService.DeviceSecretKey)).Should().Be(stagedSecret);
        (await store.GetAsync(CustodyService.StagingDeviceSecretKey)).Should().BeNull();
    }

    [Theory]
    [InlineData(false, "promoted", true)]
    [InlineData(true, "promoted", true)]
    [InlineData(false, "staged", true)]
    [InlineData(true, "staged", true)]
    [InlineData(false, "legacy", true)]
    [InlineData(false, "promoted", false)]
    [InlineData(true, "promoted", false)]
    [InlineData(false, "staged", false)]
    [InlineData(true, "staged", false)]
    [InlineData(false, "legacy", false)]
    public async Task Custody_Unlock_PublishesOnlyWhenNoLockIntervenes(
        bool deviceSecretUnlock, string sealKind, bool lockDuringUnlock)
    {
        MemStore store = new();
        PinService pin = new(store);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        await new CustodyService(store, pin).SealAsync(mnemonic, "123456");
        if (sealKind == "legacy")
        {
            await store.SetAsync(CustodyService.BlobKey, CryptoBox.Seal(CryptographyOptions.Default, mnemonic, "123456"));
            await store.RemoveAsync(CustodyService.DeviceSecretKey);
        }
        else if (sealKind == "staged")
        {
            string? secret = await store.GetAsync(CustodyService.DeviceSecretKey);
            await store.SetAsync(CustodyService.StagingDeviceSecretKey, secret!);
            await store.RemoveAsync(CustodyService.DeviceSecretKey);
        }

        // Pause after decryption, during secret promotion (or legacy re-sealing).
        GatedStore gatedStore = new(store);
        CustodyService custody = new(gatedStore, pin);
        int locked = 0;
        custody.Locked += (_, _) => locked++;

        Task<bool> unlocking = deviceSecretUnlock
            ? custody.UnlockWithDeviceSecretAsync()
            : custody.UnlockAsync("123456");
        await gatedStore.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (lockDuringUnlock)
        {
            custody.Lock();
        }

        gatedStore.ContinueWrite.SetResult();
        bool unlocked = await unlocking.WaitAsync(TimeSpan.FromSeconds(10));

        unlocked.Should().Be(!lockDuringUnlock);
        custody.ExportMnemonic().Should().Be(lockDuringUnlock ? null : mnemonic);
        custody.IsUnlocked.Should().Be(!lockDuringUnlock);
        custody.SessionExpiresAt.HasValue.Should().Be(!lockDuringUnlock);
        locked.Should().Be(lockDuringUnlock ? 1 : 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Custody_Seal_PublishesOnlyWhenNoLockIntervenes(bool lockDuringSeal)
    {
        MemStore store = new();
        PinService pin = new(store);
        GatedStore gatedStore = new(store);
        CustodyService custody = new(gatedStore, pin);
        const string mnemonic =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        int locked = 0;
        custody.Locked += (_, _) => locked++;

        Task sealing = custody.SealAsync(mnemonic, "123456");
        await gatedStore.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (lockDuringSeal)
        {
            custody.Lock();
        }

        gatedStore.ContinueWrite.SetResult();
        await sealing.WaitAsync(TimeSpan.FromSeconds(10));

        custody.ExportMnemonic().Should().Be(lockDuringSeal ? null : mnemonic);
        custody.IsUnlocked.Should().Be(!lockDuringSeal);
        custody.SessionExpiresAt.HasValue.Should().Be(!lockDuringSeal);
        locked.Should().Be(lockDuringSeal ? 1 : 0);
        (await store.GetAsync(CustodyService.StagingDeviceSecretKey)).Should().BeNull();
        CustodyService persistedCustody = new(store, pin);
        (await persistedCustody.UnlockAsync("123456")).Should().BeTrue();
        persistedCustody.ExportMnemonic().Should().Be(mnemonic);
    }

    private sealed class GatedStore(ISecureStore inner) : ISecureStore
    {
        public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ContinueWrite { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SetAsync(string key, string value)
        {
            if (key == CustodyService.DeviceSecretKey)
            {
                WriteStarted.SetResult();
                await ContinueWrite.Task;
            }

            await inner.SetAsync(key, value);
        }

        public Task<string?> GetAsync(string key) => inner.GetAsync(key);

        public Task RemoveAsync(string key) => inner.RemoveAsync(key);
    }

    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = [];

        public Task SetAsync(string key, string value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
            => Task.FromResult(_data.TryGetValue(key, out string? v) ? v : null);

        public Task RemoveAsync(string key)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
