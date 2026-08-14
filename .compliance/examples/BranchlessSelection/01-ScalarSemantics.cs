namespace ComplianceExamples.BranchlessSelection;

public static class ScalarSemantics
{
    // Prefer the semantic API. Whether the JIT uses branches is an implementation
    // detail that must be checked in the shipping runtime and architecture.
    public static int ClampIndex(int value, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        return Math.Clamp(value, 0, length - 1);
    }

    // Do not replace this with sign-bit arithmetic unless benchmarks and exhaustive
    // boundary tests prove a material advantage.
    public static int NonNegative(int value) => Math.Max(value, 0);
}
