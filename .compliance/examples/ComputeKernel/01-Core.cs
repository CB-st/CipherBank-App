namespace Example.Core;

public interface IReductionKernel
{
    Task<double> SumAsync(ReadOnlyMemory<double> values, CancellationToken cancellationToken);
}

public interface IComputeDevice
{
    bool IsAvailable { get; }
    string Name { get; }
}

public sealed class ScalarReductionKernel : IReductionKernel
{
    public Task<double> SumAsync(ReadOnlyMemory<double> values, CancellationToken cancellationToken)
    {
        var span = values.Span;
        double total = 0;
        for (var i = 0; i < span.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += span[i];
        }
        return Task.FromResult(total);
    }
}
