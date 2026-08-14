using System.Buffers;
using Example.Core;

namespace Example.Infrastructure;

public sealed class PooledReductionKernel : IReductionKernel
{
    public Task<double> SumAsync(ReadOnlyMemory<double> values, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<double>.Shared.Rent(values.Length);
        try
        {
            values.Span.CopyTo(buffer);
            double total = 0;
            for (var i = 0; i < values.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                total += buffer[i];
            }
            return Task.FromResult(total);
        }
        finally
        {
            ArrayPool<double>.Shared.Return(buffer, clearArray: true);
        }
    }
}

public sealed class AcceleratedReductionKernel(
    IComputeDevice device,
    IReductionKernel fallback) : IReductionKernel
{
    public Task<double> SumAsync(ReadOnlyMemory<double> values, CancellationToken cancellationToken)
    {
        if (!device.IsAvailable)
        {
            return fallback.SumAsync(values, cancellationToken);
        }

        // Device dispatch (buffer upload, kernel launch, result download, and
        // disposal of any device-side buffers) is library-specific. Wire the
        // chosen package's calls here; see GPU-COMPUTE.md and the library's
        // own documentation. This adapter's job is the availability check,
        // the fallback, and owning whatever device resources it allocates.
        throw new NotSupportedException(
            $"Device '{device.Name}' reported available, but no kernel dispatch is wired up in this example.");
    }
}
