using System.Numerics;
using Xunit;

namespace ComplianceExamples.BranchlessSelection;

public sealed class ThresholdVectorizedTests
{
    [Fact]
    public void Apply_matches_reference_across_vector_boundaries_and_special_values()
    {
        var widths = new[]
        {
            0,
            1,
            Math.Max(0, Vector<float>.Count - 1),
            Vector<float>.Count,
            Vector<float>.Count + 1,
            Vector<float>.Count * 4 + 3,
        };

        foreach (var length in widths.Distinct())
        {
            var input = CreateInput(length);
            var expected = new float[length];
            var actual = new float[length];

            ThresholdReference.Apply(input, expected, 0.5f, -1f, 1f);
            ThresholdVectorized.Apply(input, actual, 0.5f, -1f, 1f);

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Apply_rejects_length_mismatch()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            ThresholdVectorized.Apply([1f], new float[2], 0f, -1f, 1f));

        Assert.Equal("output", error.ParamName);
    }

    [Fact]
    public void Apply_matches_reference_for_deterministic_random_corpus()
    {
        var generator = new Random(42);
        var input = Enumerable.Range(0, 8_193)
            .Select(_ => generator.NextSingle() * 4f - 2f)
            .ToArray();
        var expected = new float[input.Length];
        var actual = new float[input.Length];

        ThresholdReference.Apply(input, expected, 0.125f, -3f, 7f);
        ThresholdVectorized.Apply(input, actual, 0.125f, -3f, 7f);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Fixed_time_token_comparison_accepts_equal_fixed_length_values()
    {
        var left = Enumerable.Range(0, FixedTimeTokenComparer.TokenSize).Select(value => (byte)value).ToArray();
        var right = left.ToArray();

        Assert.True(FixedTimeTokenComparer.EqualsToken(left, right));
        right[^1] ^= 0x01;
        Assert.False(FixedTimeTokenComparer.EqualsToken(left, right));
        Assert.False(FixedTimeTokenComparer.EqualsToken(left, right[..^1]));
    }

    private static float[] CreateInput(int length)
    {
        var edgeCases = new[]
        {
            float.NaN,
            float.NegativeInfinity,
            -0.0f,
            0.0f,
            0.49999997f,
            0.5f,
            0.50000006f,
            float.PositiveInfinity,
        };

        return Enumerable.Range(0, length)
            .Select(index => edgeCases[index % edgeCases.Length])
            .ToArray();
    }
}
