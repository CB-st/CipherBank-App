using System.Security.Cryptography;

namespace ComplianceExamples.BranchlessSelection;

public static class FixedTimeTokenComparer
{
    public const int TokenSize = 32;

    public static bool EqualsToken(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> provided)
    {
        // Token length is public protocol metadata. Reject malformed lengths before
        // the value-independent comparison instead of inventing a branchless loop.
        if (expected.Length != TokenSize || provided.Length != TokenSize)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
