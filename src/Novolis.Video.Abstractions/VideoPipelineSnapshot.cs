namespace Novolis.Video;

/// <summary>Immutable pipeline counters and latency percentiles.</summary>
public sealed record VideoPipelineSnapshot(
    long CapturedFrames,
    long EncodedFrames,
    long SentFrames,
    long ReceivedFrames,
    long PresentedFrames,
    long DroppedFrames,
    long KeyFrameRequests,
    long BytesSent,
    long BytesReceived,
    double? FrameAgeP50Milliseconds,
    double? FrameAgeP95Milliseconds,
    double? EncodeP95Milliseconds,
    double? DecodeP95Milliseconds,
    double? PresentP95Milliseconds,
    double? InputRoundTripP95Milliseconds,
    DateTimeOffset? LastFrameAt);
