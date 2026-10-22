// <copyright file="PurchaseRequestLifetimeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Purchasing;

/// <summary>
/// Regression tests for keeping an in-flight Buy purchase alive across a page return.
/// </summary>
public class PurchaseRequestLifetimeTests
{
    [Fact]
    public void BeginLoad_WhilePurchaseIsInFlight_DoesNotCancelPurchase()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken purchase = lifetime.BeginPurchase();

        CancellationToken reload = lifetime.BeginLoad();

        purchase.IsCancellationRequested.Should().BeFalse();
        reload.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void BeginLoad_ReplacesPreviousLoad_AndLeavesPurchaseRunning()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken purchase = lifetime.BeginPurchase();
        CancellationToken firstLoad = lifetime.BeginLoad();

        CancellationToken secondLoad = lifetime.BeginLoad();

        firstLoad.IsCancellationRequested.Should().BeTrue();
        secondLoad.IsCancellationRequested.Should().BeFalse();
        purchase.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void ReturnToPage_DisappearThenReload_LeavesPurchaseRunning()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken purchase = lifetime.BeginPurchase();

        lifetime.CancelWhenPageDisappears(purchaseInFlight: true);
        CancellationToken reload = lifetime.BeginLoad();

        purchase.IsCancellationRequested.Should().BeFalse();
        reload.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void RemountedLifetime_DoesNotCancelPreviousPurchase()
    {
        using PurchaseRequestLifetime original = new();
        CancellationToken purchase = original.BeginPurchase();

        using PurchaseRequestLifetime remounted = new();
        original.CancelWhenPageDisappears(purchaseInFlight: true);
        CancellationToken reload = remounted.BeginLoad();

        purchase.IsCancellationRequested.Should().BeFalse();
        reload.IsCancellationRequested.Should().BeFalse();

        remounted.CancelWhenPageDisappears(purchaseInFlight: false);
        purchase.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void CancelWhenPageDisappears_WhenNoPurchaseIsInFlight_CancelsPurchaseToken()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken purchase = lifetime.BeginPurchase();
        CancellationToken load = lifetime.BeginLoad();

        lifetime.CancelWhenPageDisappears(purchaseInFlight: false);

        load.IsCancellationRequested.Should().BeTrue();
        purchase.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void BeginPurchase_CancelsInFlightLoad()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken load = lifetime.BeginLoad();

        CancellationToken purchase = lifetime.BeginPurchase();

        load.IsCancellationRequested.Should().BeTrue();
        purchase.IsCancellationRequested.Should().BeFalse();
        load.Invoking(static token => token.ThrowIfCancellationRequested())
            .Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void BeginLoad_ReleasesTheReplacedLoadSource()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken firstLoad = lifetime.BeginLoad();

        _ = lifetime.BeginLoad();
        _ = lifetime.BeginPurchase();

        // A disposed source still reports cancellation but no longer hands out a wait handle.
        firstLoad.IsCancellationRequested.Should().BeTrue();
        firstLoad.Invoking(static token => token.WaitHandle).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void BeginPurchase_ReleasesThePreviousPurchaseSource()
    {
        using PurchaseRequestLifetime lifetime = new();
        CancellationToken firstPurchase = lifetime.BeginPurchase();

        _ = lifetime.BeginPurchase();

        firstPurchase.IsCancellationRequested.Should().BeTrue();
        firstPurchase.Invoking(static token => token.WaitHandle).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_DoesNotCancelInFlightPurchase()
    {
        PurchaseRequestLifetime lifetime = new();
        CancellationToken purchase = lifetime.BeginPurchase();

        lifetime.Dispose();

        purchase.IsCancellationRequested.Should().BeFalse();
        purchase.Invoking(static token => token.ThrowIfCancellationRequested()).Should().NotThrow();
        using CancellationTokenRegistration registration = purchase.Register(static () => { });
        registration.Dispose();
    }
}
