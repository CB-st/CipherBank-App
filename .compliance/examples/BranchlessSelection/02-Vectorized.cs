using System.Numerics;

namespace ComplianceExamples.BranchlessSelection;

public static class ThresholdVectorized
{
    public static void Apply(
        ReadOnlySpan<float> input,
        Span<float> output,
        float threshold,
        float lowValue,
        float highValue)
    {
        ThresholdReference.EnsureSameLength(input, output);

        if (!Vector.IsHardwareAccelerated || input.Length < Vector<float>.Count)
        {
            ThresholdReference.Apply(input, output, threshold, lowValue, highValue);
            return;
        }

        var width = Vector<float>.Count;
        var thresholdVector = new Vector<float>(threshold);
        var lowVector = new Vector<float>(lowValue);
        var highVector = new Vector<float>(highValue);
        var vectorizedLength = input.Length - input.Length % width;

        var index = 0;
        for (; index < vectorizedLength; index += width)
        {
            var values = new Vector<float>(input.Slice(index, width));
            var mask = Vector.GreaterThanOrEqual(values, thresholdVector);
            var selected = Vector.ConditionalSelect(mask, highVector, lowVector);
            selected.CopyTo(output.Slice(index, width));
        }

        for (; index < input.Length; index++)
        {
            output[index] = input[index] >= threshold ? highValue : lowValue;
        }
    }
}
