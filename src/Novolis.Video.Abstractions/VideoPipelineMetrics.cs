namespace Novolis.Video;

/// <summary>
/// Keeps bounded, thread-safe pipeline counters and latency samples.
/// </summary>
public sealed class VideoPipelineMetrics
{
    private readonly LatencySampleWindow _frameAge = new();
    private readonly LatencySampleWindow _encode = new();
    private readonly LatencySampleWindow _decode = new();
    private readonly LatencySampleWindow _present = new();
    private readonly LatencySampleWindow _inputRoundTrip = new();
    private long _capturedFrames;
    private long _encodedFrames;
    private long _sentFrames;
    private long _receivedFrames;
    private long _presentedFrames;
    private long _droppedFrames;
    private long _keyFrameRequests;
    private long _bytesSent;
    private long _bytesReceived;
    private long _lastFrameTicks;

    /// <summary>Records a frame observed by the capture source.</summary>
    public void RecordCaptured() => Interlocked.Increment(ref _capturedFrames);

    /// <summary>Records one encoded frame and its encode duration.</summary>
    public void RecordEncoded(double durationMilliseconds)
    {
        Interlocked.Increment(ref _encodedFrames);
        _encode.Add(durationMilliseconds);
    }

    /// <summary>Records a frame sent by a transport boundary.</summary>
    public void RecordSent(int bytes)
    {
        Interlocked.Increment(ref _sentFrames);
        Interlocked.Add(ref _bytesSent, Math.Max(0, bytes));
    }

    /// <summary>Records a received frame and, when available, its source age.</summary>
    public void RecordReceived(int bytes, long? sourceUtcTicks = null)
    {
        Interlocked.Increment(ref _receivedFrames);
        Interlocked.Add(ref _bytesReceived, Math.Max(0, bytes));
        var now = DateTimeOffset.UtcNow;
        Interlocked.Exchange(ref _lastFrameTicks, now.UtcTicks);
        if (sourceUtcTicks is { } source
            && source > 0
            && source <= now.UtcTicks)
        {
            var age = TimeSpan.FromTicks(now.UtcTicks - source).TotalMilliseconds;
            if (age is >= 0 and <= 60_000)
                _frameAge.Add(age);
        }
    }

    /// <summary>Records one decoded frame and its decoder duration.</summary>
    public void RecordDecoded(double durationMilliseconds) =>
        _decode.Add(durationMilliseconds);

    /// <summary>Records one presented frame and its presentation duration.</summary>
    public void RecordPresented(
        double durationMilliseconds,
        long? sourceUtcTicks = null)
    {
        Interlocked.Increment(ref _presentedFrames);
        _present.Add(durationMilliseconds);
        if (sourceUtcTicks is { } source)
        {
            var now = DateTimeOffset.UtcNow;
            if (source > 0 && source <= now.UtcTicks)
            {
                var age = TimeSpan.FromTicks(now.UtcTicks - source).TotalMilliseconds;
                if (age is >= 0 and <= 60_000)
                    _frameAge.Add(age);
            }
        }
    }

    /// <summary>Records a frame evicted by a bounded media queue.</summary>
    public void RecordDropped() => Interlocked.Increment(ref _droppedFrames);

    /// <summary>Records a request for a fresh intra frame.</summary>
    public void RecordKeyFrameRequest() =>
        Interlocked.Increment(ref _keyFrameRequests);

    /// <summary>Records the time spent waiting to send input.</summary>
    public void RecordInputRoundTrip(double durationMilliseconds) =>
        _inputRoundTrip.Add(durationMilliseconds);

    /// <summary>Returns a consistent-enough bounded snapshot for diagnostics.</summary>
    public VideoPipelineSnapshot Snapshot() => new(
        Interlocked.Read(ref _capturedFrames),
        Interlocked.Read(ref _encodedFrames),
        Interlocked.Read(ref _sentFrames),
        Interlocked.Read(ref _receivedFrames),
        Interlocked.Read(ref _presentedFrames),
        Interlocked.Read(ref _droppedFrames),
        Interlocked.Read(ref _keyFrameRequests),
        Interlocked.Read(ref _bytesSent),
        Interlocked.Read(ref _bytesReceived),
        _frameAge.Percentile(0.50),
        _frameAge.Percentile(0.95),
        _encode.Percentile(0.95),
        _decode.Percentile(0.95),
        _present.Percentile(0.95),
        _inputRoundTrip.Percentile(0.95),
        ReadLastFrameAt());

    private DateTimeOffset? ReadLastFrameAt()
    {
        var ticks = Interlocked.Read(ref _lastFrameTicks);
        return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
