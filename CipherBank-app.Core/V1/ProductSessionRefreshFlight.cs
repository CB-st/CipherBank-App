// <copyright file="ProductSessionRefreshFlight.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>One rotating refresh and its independently cancellable waiters.</summary>
internal sealed class ProductSessionRefreshFlight : IDisposable
{
    internal ProductSessionRefreshFlight(ProductSessionSnapshot original)
    {
        Original = original;
        Cancellation = CancellationTokenSource.CreateLinkedTokenSource(original.Operation.Token);
    }

    internal ProductSessionSnapshot Original { get; }

    internal CancellationTokenSource Cancellation { get; }

    internal TaskCompletionSource<ProductSessionSnapshot> CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<ProductSessionSnapshot> Completion => CompletionSource.Task;

    internal int Waiters { get; set; }

    public void Dispose() => Cancellation.Dispose();
}
