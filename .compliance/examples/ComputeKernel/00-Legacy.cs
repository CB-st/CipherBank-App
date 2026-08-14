// Legacy example: a per-call scratch allocation with no pooling, and an inline
// device check with no availability verification, fallback, or disposal discipline.
public static class LegacyReducer
{
    public static double Sum(double[] values)
    {
        var buffer = new double[values.Length];
        Array.Copy(values, buffer, values.Length);

        if (CudaInterop.HasDevice())
        {
            return CudaInterop.SumOnDevice(buffer);
        }

        double total = 0;
        foreach (var value in buffer)
        {
            total += value;
        }
        return total;
    }
}
