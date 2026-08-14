using Example.Core;
using Example.Infrastructure;
using NSubstitute;

namespace Example.Tests;

public sealed class ReductionKernelTests
{
    [Fact]
    public async Task ScalarAndPooledKernels_AgreeOnRepresentativeInput()
    {
        double[] values = [1.5, 2.5, -3.0, 100.25, 0.0];
        var scalar = new ScalarReductionKernel();
        var pooled = new PooledReductionKernel();

        var scalarTotal = await scalar.SumAsync(values, TestContext.Current.CancellationToken);
        var pooledTotal = await pooled.SumAsync(values, TestContext.Current.CancellationToken);

        Assert.Equal(scalarTotal, pooledTotal, precision: 12);
    }

    [Fact]
    public async Task AcceleratedKernel_FallsBackToScalarKernel_WhenDeviceUnavailable()
    {
        var device = Substitute.For<IComputeDevice>();
        device.IsAvailable.Returns(false);
        var fallback = Substitute.For<IReductionKernel>();
        fallback.SumAsync(Arg.Any<ReadOnlyMemory<double>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(6.0));
        var accelerated = new AcceleratedReductionKernel(device, fallback);
        double[] values = [1.0, 2.0, 3.0];

        var total = await accelerated.SumAsync(values, TestContext.Current.CancellationToken);

        Assert.Equal(6.0, total);
        await fallback.Received(1).SumAsync(values, TestContext.Current.CancellationToken);
    }
}
