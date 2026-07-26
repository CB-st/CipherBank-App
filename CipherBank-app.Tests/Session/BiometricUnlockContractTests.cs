// <copyright file="BiometricUnlockContractTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Session;

public class BiometricUnlockContractTests
{
    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = new();

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

    [Fact]
    public async Task Seal_StoresDeviceSecret_AndUnlockWithDeviceSecret_WorksWithoutPin()
    {
        var store = new MemStore();
        var custody = new CustodyService(store, new PinService(store));
        string mnemonic = MnemonicHelper.Generate();
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
        string mnemonic = MnemonicHelper.Normalize(MnemonicHelper.Generate());
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
    public async Task UnlockWithDeviceSecret_FailsWhenMissing()
    {
        var store = new MemStore();
        var custody = new CustodyService(store, new PinService(store));
        (await custody.UnlockWithDeviceSecretAsync()).Should().BeFalse();
    }
}
