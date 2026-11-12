// <copyright file="ProductSessionRefreshWaiter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Releases one refresh admission exactly once, including synchronous caller cancellation.</summary>
internal sealed class ProductSessionRefreshWaiter
{
    private int _released;

    internal bool TryRelease() => Interlocked.Exchange(ref _released, 1) == 0;
}
