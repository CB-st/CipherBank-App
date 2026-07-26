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

    [Fact]
    public async Task Change_RejectsConfirmMismatch_WithoutTouchingStoredPin()
    {
        (PinService? pin, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync(CurrentPin, NextPin, NextPin + "9");

        outcome.Status.Should().Be(PinChangeStatus.Mismatch);
        outcome.Succeeded.Should().BeFalse();
        outcome.Message.Should().NotBeNullOrWhiteSpace();
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue("a rejected change must leave the old PIN active");
    }

    [Fact]
    public async Task Change_RejectsTooShortNewPin()
    {
        (PinService? pin, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync(CurrentPin, "1234", "1234");

        outcome.Status.Should().Be(PinChangeStatus.TooShort);
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();
    }

    [Fact]
    public async Task Change_RejectsWrongCurrentPin()
    {
        (PinService? pin, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync("999999", NextPin, NextPin);

        outcome.Status.Should().Be(PinChangeStatus.WrongCurrentPin);
        (await pin.VerifyPinAsync(NextPin)).Should().BeFalse("the new PIN must not be armed when the old one fails");
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();
    }

    [Fact]
    public async Task Change_RejectsReusingCurrentPin()
    {
        (PinService _, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync(CurrentPin, CurrentPin, CurrentPin);

        outcome.Status.Should().Be(PinChangeStatus.SameAsCurrent);
    }

    [Fact]
    public async Task Change_SucceedsAndSwapsActivePin()
    {
        (PinService? pin, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync(CurrentPin, NextPin, NextPin);

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

    /// <summary>
    /// The device-secret invariant: a legacy PIN-derived blob (no <c>cb_device_secret_v1</c>) can only be
    /// migrated by <see cref="CustodyService.UnlockAsync"/>, so custody must refuse a PIN change that would
    /// swap the hash and orphan that blob — and the old PIN must still open it afterwards.
    /// </summary>
    [Fact]
    public async Task Change_RefusedOnLegacyBlob_WithoutDeviceSecret_PreservesOldPin()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        string mnemonic = await SeedLegacyPinDerivedBlobAsync(store, pin);

        PinChangeOutcome outcome = await new PinChangeCoordinator(custody).ChangeAsync(CurrentPin, NextPin, NextPin);

        outcome.Succeeded.Should().BeFalse();
        outcome.Status.Should().Be(PinChangeStatus.VaultNotReady);
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue("the refused change must leave the old PIN active");
        (await pin.VerifyPinAsync(NextPin)).Should().BeFalse();
        (await custody.UnlockAsync(CurrentPin)).Should().BeTrue("the legacy blob is still openable with the old PIN");
        custody.ExportMnemonic().Should().Be(mnemonic);
    }

    /// <summary>
    /// Once an unlock has migrated the legacy blob to a device secret, the same change is allowed and the
    /// mnemonic survives the PIN swap (the blob is keyed by the device secret, never re-sealed).
    /// </summary>
    [Fact]
    public async Task Change_AllowedAfterMigration_KeepsMnemonicReachableWithNewPin()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        string mnemonic = await SeedLegacyPinDerivedBlobAsync(store, pin);
        (await custody.UnlockAsync(CurrentPin)).Should().BeTrue();

        PinChangeOutcome outcome = await new PinChangeCoordinator(custody).ChangeAsync(CurrentPin, NextPin, NextPin);

        outcome.Succeeded.Should().BeTrue();
        custody.Lock();
        (await custody.UnlockAsync(NextPin)).Should().BeTrue();
        custody.ExportMnemonic().Should().Be(mnemonic);
    }

    /// <summary>
    /// <see cref="CustodyService.ChangePinAsync"/> reports the invariant breach distinctly from a wrong PIN,
    /// so callers cannot mistake "refused" for "bad credentials".
    /// </summary>
    [Fact]
    public async Task CustodyChangePin_ReportsDeviceSecretMissing_DistinctFromWrongPin()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        await SeedLegacyPinDerivedBlobAsync(store, pin);

        (await custody.ChangePinAsync(CurrentPin, NextPin))
            .Should().Be(CustodyPinChangeResult.DeviceSecretMissing);

        (await custody.UnlockAsync(CurrentPin)).Should().BeTrue();
        (await custody.ChangePinAsync("000000", NextPin)).Should().Be(CustodyPinChangeResult.WrongPin);
        (await custody.ChangePinAsync(CurrentPin, NextPin)).Should().Be(CustodyPinChangeResult.Changed);
    }

    [Theory]
    [InlineData(null, null, null, PinChangeStatus.TooShort)]
    [InlineData(CurrentPin, null, NextPin, PinChangeStatus.TooShort)]
    [InlineData(CurrentPin, NextPin, null, PinChangeStatus.Mismatch)]
    [InlineData(null, NextPin, NextPin, PinChangeStatus.Success)]
    public void ValidateShape_ToleratesNullInputs(
        string? currentPin, string? newPin, string? confirmPin, PinChangeStatus expected)
        => PinChangeCoordinator.ValidateShape(currentPin, newPin, confirmPin).Should().Be(expected);

    [Fact]
    public async Task ChangeAsync_WithNullNewPin_IsRejectedWithoutTouchingStoredPin()
    {
        (PinService? pin, PinChangeCoordinator? coordinator) = await SeededAsync();

        PinChangeOutcome outcome = await coordinator.ChangeAsync(CurrentPin, null, null);

        outcome.Status.Should().Be(PinChangeStatus.TooShort);
        (await pin.VerifyPinAsync(CurrentPin)).Should().BeTrue();
    }

    /// <summary>
    /// Builds a fully sealed custody (mnemonic + device secret + <see cref="CurrentPin"/>) plus a coordinator
    /// over it, i.e. the state a real device is in when Change PIN is reachable.
    /// Use: High (every Fact here). Scope: single test.
    /// </summary>
    private static async Task<(PinService Pin, PinChangeCoordinator Coordinator)> SeededAsync()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        await custody.SealAsync(MnemonicHelper.Generate(), CurrentPin);
        return (pin, new PinChangeCoordinator(custody));
    }

    /// <summary>
    /// Writes the pre-device-secret shape straight into the store: a blob sealed with the PIN itself plus a
    /// PIN hash, exactly what <see cref="CustodyService.UnlockAsync"/>'s legacy branch expects.
    /// Use: Medium (legacy-invariant Facts). Scope: single test.
    /// </summary>
    private static async Task<string> SeedLegacyPinDerivedBlobAsync(MemStore store, PinService pin)
    {
        string mnemonic = MnemonicHelper.Generate();
        await pin.SetPinAsync(CurrentPin);
        await store.SetAsync(CustodyService.BlobKey, CryptoBox.Seal(mnemonic, CurrentPin));
        return mnemonic;
    }

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
}
