// <copyright file="PurchaseRequestLifetime.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Purchasing;

/// <summary>
/// Cancellation for a Buy catalog reload and for the purchase request.
/// A reload, a page return, or a new page instance must not cancel an in-flight purchase.
/// </summary>
internal sealed class PurchaseRequestLifetime : IDisposable
{
    private readonly List<CancellationTokenSource> _retired = [];

    private CancellationTokenSource? _load;
    private CancellationTokenSource? _purchase;
    private bool _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _load?.Dispose();
            _purchase?.Dispose();
            _load = null;
            _purchase = null;

            foreach (CancellationTokenSource source in _retired)
            {
                source.Dispose();
            }

            _retired.Clear();
        }

        GC.SuppressFinalize(this);
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

    private CancellationToken Replace(ref CancellationTokenSource? slot)
    {
        Retire(ref slot);
        CancellationTokenSource next = new();
        slot = next;
        return next.Token;
    }

    private void Retire(ref CancellationTokenSource? slot)
    {
        if (slot is null)
        {
            return;
        }

        slot.Cancel();
        _retired.Add(slot);
        slot = null;
    }
}
