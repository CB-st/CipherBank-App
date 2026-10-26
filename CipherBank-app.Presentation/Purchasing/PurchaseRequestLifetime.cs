// <copyright file="PurchaseRequestLifetime.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank.Presentation.Purchasing;

/// <summary>
/// Cancellation for a Buy catalog reload and for the purchase request.
/// A reload, a page return, or a new page instance must not cancel an in-flight purchase.
/// </summary>
internal sealed class PurchaseRequestLifetime : IDisposable
{
    private CancellationTokenSource? _load;
    private CancellationTokenSource? _purchase;
    private bool _disposed;

    /// <summary>
    /// Releases the request cancellation sources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _load?.Dispose();
            _purchase?.Dispose();
            _load = null;
            _purchase = null;
        }
    }

    /// <summary>
    /// Starts a catalog reload. Cancels only the previous reload.
    /// </summary>
    /// <returns>The token for this reload.</returns>
    internal CancellationToken BeginLoad()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Replace(ref _load);
    }

    /// <summary>
    /// Starts a purchase. Cancels an in-flight reload and any previous purchase.
    /// </summary>
    /// <returns>The token for this purchase.</returns>
    internal CancellationToken BeginPurchase()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Retire(ref _load);
        return Replace(ref _purchase);
    }

    /// <summary>
    /// Cancels the catalog reload when the page leaves.
    /// The purchase is cancelled only when one is not in flight.
    /// </summary>
    /// <param name="purchaseInFlight">True while a purchase must keep running.</param>
    internal void CancelWhenPageDisappears(bool purchaseInFlight)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _load?.Cancel();
        if (!purchaseInFlight)
        {
            _purchase?.Cancel();
        }
    }

    private static CancellationToken Replace(ref CancellationTokenSource? slot)
    {
        Retire(ref slot);
        CancellationTokenSource next = new();
        slot = next;
        return next.Token;
    }

    private static void Retire(ref CancellationTokenSource? slot)
    {
        if (slot is null)
        {
            return;
        }

        // Cancel runs the operation's callbacks first. Its token still reports
        // cancellation after Dispose, so the source is released here rather than
        // kept until the page's view model is disposed.
        slot.Cancel();
        slot.Dispose();
        slot = null;
    }
}
