namespace Novolis.Video;

/// <summary>One encoded access unit ready for a media transport.</summary>
public sealed record EncodedVideoFrame(
    int Width,
    int Height,
    long Timestamp,
    byte[] AccessUnit,
    bool IsKeyFrame,
    string Codec = "H264");
