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
        byte[] key = box.DeriveKey(pin, salt);
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
        await store.SetAsync(CustodyService.BlobKey, CryptoBox.Seal(mnemonic, stagedSecret));

        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
        (await store.GetAsync(CustodyService.DeviceSecretKey)).Should().Be(stagedSecret);
        (await store.GetAsync(CustodyService.StagingDeviceSecretKey)).Should().BeNull();
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
}
