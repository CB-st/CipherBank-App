namespace ComplianceExamples.BranchlessSelection;

public static class ThresholdReference
{
    public static void Apply(
        ReadOnlySpan<float> input,
        Span<float> output,
        float threshold,
        float lowValue,
        float highValue)
    {
        EnsureSameLength(input, output);

        for (var index = 0; index < input.Length; index++)
        {
            output[index] = input[index] >= threshold ? highValue : lowValue;
        }
    }

    internal static void EnsureSameLength(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != output.Length)
        {
            throw new ArgumentException("Input and output must have the same length.", nameof(output));
        }
    }
}
