namespace Novolis.Video;

/// <summary>A bounded, thread-safe ring of latency samples in milliseconds.</summary>
public sealed class LatencySampleWindow
{
    private readonly long[] _samples;
    private int _next;
    private int _count;

    /// <summary>Creates a window that retains the newest samples.</summary>
    public LatencySampleWindow(int capacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(capacity, 0);
        _samples = new long[capacity];
    }

    /// <summary>Records one non-negative millisecond sample.</summary>
    public void Add(double milliseconds)
    {
        if (double.IsNaN(milliseconds)
            || double.IsInfinity(milliseconds)
            || milliseconds < 0)
        {
            return;
        }

        var micros = (long)Math.Clamp(
            Math.Round(milliseconds * 1_000d),
            0,
            long.MaxValue);
        var index = Interlocked.Increment(ref _next) - 1;
        Interlocked.Exchange(ref _samples[index % _samples.Length], micros);
        var count = Volatile.Read(ref _count);
        while (count < _samples.Length
            && Interlocked.CompareExchange(
                ref _count,
                count + 1,
                count) != count)
        {
            count = Volatile.Read(ref _count);
        }
    }

    /// <summary>Returns a percentile in milliseconds, or null when empty.</summary>
    public double? Percentile(double percentile)
    {
        var count = Volatile.Read(ref _count);
        if (count == 0)
            return null;

        var values = new long[count];
        for (var index = 0; index < count; index++)
            values[index] = Volatile.Read(ref _samples[index]);
        Array.Sort(values);
        var selected = (int)Math.Clamp(
            Math.Ceiling(percentile * values.Length) - 1,
            0,
            values.Length - 1);
        return values[selected] / 1_000d;
    }
}
