// <copyright file="CustodyAccountKeySourceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.ChallengePass;

public sealed class CustodyAccountKeySourceTests
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
    public async Task Unlocked_custody_derives_stable_a1_and_hybrid_keys()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        string mnemonic = MnemonicHelper.Generate();
        await custody.SealAsync(mnemonic, "123456");
        (await custody.UnlockAsync("123456")).Should().BeTrue();

        var source = new CustodyAccountKeySource(custody);
        var algo = new X25519ChaChaSealAlgorithm();
        var a = source.RequireUnlockedKeyPair(algo);
        var b = source.RequireUnlockedKeyPair(algo);
        a.PublicKey.Should().Equal(b.PublicKey);

        HybridPrivateIdentity h1 = source.RequireHybridIdentity();
        HybridPrivateIdentity h2 = source.RequireHybridIdentity();
        h1.X25519PublicKey.Should().Equal(h2.X25519PublicKey);
        h1.MlKemPublicKey.Should().Equal(h2.MlKemPublicKey);
    }

    [Fact]
    public void Locked_custody_throws()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var source = new CustodyAccountKeySource(custody);
        Assert.Throws<InvalidOperationException>(() => source.RequireHybridIdentity());
    }
}
