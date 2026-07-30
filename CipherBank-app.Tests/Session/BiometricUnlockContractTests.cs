// <copyright file="BiometricUnlockContractTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Session;

public class BiometricUnlockContractTests
{
    [Fact]
    public async Task Seal_StoresDeviceSecret_AndUnlockWithDeviceSecret_WorksWithoutPin()
    {
        var store = new MemStore();
        var custody = new CustodyService(store, new PinService(store));
        var mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        custody.Lock();

        (await custody.CanUnlockWithDeviceOwnerAsync()).Should().BeTrue();
        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(MnemonicHelper.Normalize(mnemonic));
    }

    [Fact]
    public async Task LegacyPinBlob_MigratesOnPinUnlock_ThenDeviceSecretWorks()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        await pin.SetPinAsync("654321");
        var mnemonic = MnemonicHelper.Normalize(MnemonicHelper.Generate());
        await store.SetAsync("cb_custody_blob", CryptoBox.Seal(mnemonic, "654321"));

        var custody = new CustodyService(store, pin);
        (await custody.CanUnlockWithDeviceOwnerAsync()).Should().BeFalse();
        (await custody.UnlockAsync("654321")).Should().BeTrue();
        custody.Lock();

        (await custody.CanUnlockWithDeviceOwnerAsync()).Should().BeTrue();
        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
    }

    [Fact]
    public async Task InterruptedMigration_DeviceSecretWithoutRewrittenBlob_RecoversViaPin()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        await pin.SetPinAsync("246810");
        var mnemonic = MnemonicHelper.Normalize(MnemonicHelper.Generate());
        await store.SetAsync(CustodyService.BlobKey, CryptoBox.Seal(mnemonic, "246810"));

        // Simulate old bug: device secret persisted, blob still PIN-sealed.
        await store.SetAsync(CustodyService.DeviceSecretKey, Convert.ToBase64String(new byte[32]));

        var custody = new CustodyService(store, pin);
        (await custody.UnlockAsync("246810")).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
        custody.Lock();

        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
    }

    [Fact]
    public async Task InterruptedMigration_StagedSecretBeforePromote_UnlocksWithDeviceSecret()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        await pin.SetPinAsync("135791");
        var mnemonic = MnemonicHelper.Normalize(MnemonicHelper.Generate());
        var deviceSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await store.SetAsync(CustodyService.StagingDeviceSecretKey, deviceSecret);
        await store.SetAsync(CustodyService.BlobKey, CryptoBox.Seal(mnemonic, deviceSecret));

        // DeviceSecretKey never promoted.
        var custody = new CustodyService(store, pin);
        (await custody.CanUnlockWithDeviceOwnerAsync()).Should().BeTrue();
        (await custody.UnlockWithDeviceSecretAsync()).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
        (await store.GetAsync(CustodyService.DeviceSecretKey)).Should().Be(deviceSecret);
        (await store.GetAsync(CustodyService.StagingDeviceSecretKey)).Should().BeNull();
    }

    [Fact]
    public async Task UnlockWithDeviceSecret_FailsWhenMissing()
    {
        var store = new MemStore();
        var custody = new CustodyService(store, new PinService(store));
        (await custody.UnlockWithDeviceSecretAsync()).Should().BeFalse();
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
            => Task.FromResult(_data.TryGetValue(key, out var v) ? v : null);

        public Task RemoveAsync(string key)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }
    }
}
