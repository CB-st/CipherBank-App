// <copyright file="PinAndCustodyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class PinAndCustodyTests
{
    [Fact]
    public async Task Pin_VerifySucceedsAfterSet()
    {
        var pin = new PinService(new MemStore());
        await pin.SetPinAsync("654321");
        (await pin.VerifyPinAsync("654321")).Should().BeTrue();
        (await pin.VerifyPinAsync("000000")).Should().BeFalse();
    }

    [Fact]
    public async Task Custody_SealUnlockRoundTrip()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        custody.Lock();
        (await custody.UnlockAsync("123456")).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(MnemonicHelper.Normalize(mnemonic));
    }

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
}
