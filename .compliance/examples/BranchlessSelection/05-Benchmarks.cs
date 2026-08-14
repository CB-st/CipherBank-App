using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace ComplianceExamples.BranchlessSelection;

[MemoryDiagnoser]
[DisassemblyDiagnoser(printSource: true, maxDepth: 3)]
public sealed class ThresholdBenchmarks
{
    [Params(32, 1_024, 1_048_576)]
    public int Length { get; set; }

    private float[] random = [];
    private float[] skewed = [];
    private float[] sorted = [];
    private float[] output = [];

    [GlobalSetup]
    public void Setup()
    {
        var generator = new Random(42);
        random = Enumerable.Range(0, Length).Select(_ => generator.NextSingle()).ToArray();
        skewed = Enumerable.Range(0, Length).Select(index => index % 100 == 0 ? 1f : 0f).ToArray();
        sorted = Enumerable.Range(0, Length).Select(index => index < Length / 2 ? 0f : 1f).ToArray();
        output = new float[Length];
    }

    [Benchmark(Baseline = true)]
    public float[] BranchyRandom()
    {
        ThresholdReference.Apply(random, output, 0.5f, -1f, 1f);
        return output;
    }

    [Benchmark]
    public float[] MaskedRandom()
    {
        ThresholdVectorized.Apply(random, output, 0.5f, -1f, 1f);
        return output;
    }

    [Benchmark]
    public float[] BranchySkewed()
    {
        ThresholdReference.Apply(skewed, output, 0.5f, -1f, 1f);
        return output;
    }

    [Benchmark]
    public float[] MaskedSkewed()
    {
        ThresholdVectorized.Apply(skewed, output, 0.5f, -1f, 1f);
        return output;
    }

    [Benchmark]
    public float[] BranchySorted()
    {
        ThresholdReference.Apply(sorted, output, 0.5f, -1f, 1f);
        return output;
    }

    [Benchmark]
    public float[] MaskedSorted()
    {
        ThresholdVectorized.Apply(sorted, output, 0.5f, -1f, 1f);
        return output;
    }
}
