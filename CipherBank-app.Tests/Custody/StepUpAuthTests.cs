// <copyright file="StepUpAuthTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class StepUpAuthTests
{
    [Fact]
    public async Task RequireAsync_False_WhenPinCancelled()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        await pin.SetPinAsync("111111");
        var challenges = new FakeChallenges { BiometricsPreferred = false, PinPromptResult = null };
        var step = new StepUpAuthService(challenges, pin);

        (await step.RequireAsync(AuthReason.Payment, default)).Should().BeFalse();
    }

    [Fact]
    public async Task RequireAsync_True_WhenBiometricsSucceed()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var challenges = new FakeChallenges { BiometricsPreferred = true, BioSucceed = true };
        var step = new StepUpAuthService(challenges, pin);

        (await step.RequireAsync(AuthReason.Convert, default)).Should().BeTrue();
    }

    [Fact]
    public async Task RequireAsync_True_WhenCorrectPinEntered()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        await pin.SetPinAsync("222222");
        var challenges = new FakeChallenges { BiometricsPreferred = false, PinPromptResult = "222222" };
        var step = new StepUpAuthService(challenges, pin);

        (await step.RequireAsync(AuthReason.RevealKeys, default)).Should().BeTrue();
    }

    private sealed class FakeChallenges : IStepUpChallenges
    {
        public bool BiometricsPreferred { get; set; }

        public bool BioSucceed { get; set; }

        public string? PinPromptResult { get; set; }

        public Task<bool> TryBiometricsAsync(string prompt, CancellationToken ct)
            => Task.FromResult(BioSucceed);

        public Task<string?> PromptForPinAsync(string prompt, CancellationToken ct)
            => Task.FromResult(PinPromptResult);
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
