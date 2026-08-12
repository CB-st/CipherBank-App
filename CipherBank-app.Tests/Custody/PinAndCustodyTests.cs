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
