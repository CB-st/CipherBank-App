// <copyright file="PinAndCustodyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
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
    public async Task Custody_SealAsync_RejectsShortPin()
    {
        MemStore store = new MemStore();
        PinService pin = new PinService(store);
        CustodyService custody = new CustodyService(store, pin);
        Func<Task> act = () => custody.SealAsync(MnemonicHelper.Generate(), "12");
        await act.Should().ThrowAsync<ArgumentException>();
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
