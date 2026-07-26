// <copyright file="PinChangeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

/// <summary>
/// Drives the change-PIN decision logic that <c>ChangePinViewModel</c> binds to: shape validation
/// (length / confirm match / reuse) and the verify-then-replace path through <see cref="IPinService"/>.
/// The ViewModel itself is a thin binder over <see cref="PinChangeCoordinator"/>, so these tests are the
/// unit coverage for the Change-PIN feature (the MAUI project cannot be referenced from net10.0 tests).
/// </summary>
public class PinChangeTests
{
    private const string CurrentPin = "246810";
    private const string NextPin = "135791";

    /// <summary>
    /// In-memory <see cref="ISecureStore"/> so PIN hash/salt/lockout round-trip without platform storage.
    /// Use: High (every Fact here). Scope: one test's PinService instance.
    /// </summary>
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

    /// <summary>
    /// Builds a PinService seeded with <see cref="CurrentPin"/> plus a coordinator over it.
    /// Use: High (every Fact here). Scope: single test.
    /// </summary>
    private static async Task<(PinService Pin, PinChangeCoordinator Coordinator)> SeededAsync()
    {
        var pin = new PinService(new MemStore());
        await pin.SetPinAsync(CurrentPin);
        return (pin, new PinChangeCoordinator(pin));
    }

    [Fact]
    public async Task Change_RejectsConfirmMismatch_WithoutTouchingStoredPin()
    {
        var (pin, coordinator) = await SeededAsync();

        var outcome = await coordinator.ChangeAsync(CurrentPin, NextPin, NextPin + "9");

        outcome.Status.Should().Be(PinChangeStatus.Mismatch);
        outcome.Succeeded.Should().BeFalse();
        outcome.Message.Should().NotBeNullOrWhiteSpace();
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue("a rejected change must leave the old PIN active");
    }

    [Fact]
    public async Task Change_RejectsTooShortNewPin()
    {
        var (pin, coordinator) = await SeededAsync();

        var outcome = await coordinator.ChangeAsync(CurrentPin, "1234", "1234");

        outcome.Status.Should().Be(PinChangeStatus.TooShort);
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();
    }

    [Fact]
    public async Task Change_RejectsWrongCurrentPin()
    {
        var (pin, coordinator) = await SeededAsync();

        var outcome = await coordinator.ChangeAsync("999999", NextPin, NextPin);

        outcome.Status.Should().Be(PinChangeStatus.WrongCurrentPin);
        (await pin.VerifyPinAsync(NextPin)).Should().BeFalse("the new PIN must not be armed when the old one fails");
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();
    }

    [Fact]
    public async Task Change_RejectsReusingCurrentPin()
    {
        var (_, coordinator) = await SeededAsync();

        var outcome = await coordinator.ChangeAsync(CurrentPin, CurrentPin, CurrentPin);

        outcome.Status.Should().Be(PinChangeStatus.SameAsCurrent);
    }

    [Fact]
    public async Task Change_SucceedsAndSwapsActivePin()
    {
        var (pin, coordinator) = await SeededAsync();

        var outcome = await coordinator.ChangeAsync(CurrentPin, NextPin, NextPin);

        outcome.Succeeded.Should().BeTrue();
        outcome.Status.Should().Be(PinChangeStatus.Success);
        (await pin.VerifyPinAsync(NextPin)).Should().BeTrue("the new PIN unlocks after a successful change");
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeFalse("the old PIN stops working after a successful change");
    }

    [Fact]
    public async Task ChangePinAsync_OnPinService_VerifiesBeforeReplacing()
    {
        var pin = new PinService(new MemStore());
        await pin.SetPinAsync(CurrentPin);

        (await pin.ChangePinAsync("000000", NextPin)).Should().BeFalse();
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();

        (await pin.ChangePinAsync(CurrentPin, NextPin)).Should().BeTrue();
        (await pin.VerifyPinAsync(NextPin)).Should().BeTrue();
    }
}
