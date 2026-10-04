namespace Novolis.Video;

/// <summary>Negotiated dimensions and pacing for one video stream.</summary>
public sealed record VideoAdaptiveProfile(
    VideoAdaptiveProfileKind Kind,
    int Width,
    int Height,
    int FramesPerSecond,
    int TargetBitrate)
{
    /// <summary>Creates a conservative profile for a source size.</summary>
    public static VideoAdaptiveProfile ForSource(
        int sourceWidth,
        int sourceHeight,
        VideoAdaptiveProfileKind kind)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceHeight);
        var maximumWidth = kind switch
        {
            VideoAdaptiveProfileKind.High => 960,
            VideoAdaptiveProfileKind.Medium => 720,
            _ => 480,
        };
        var scale = Math.Min(1d, maximumWidth / (double)sourceWidth);
        var width = Align(sourceWidth * scale);
        var height = Align(sourceHeight * scale);
        var framesPerSecond = kind switch
        {
            VideoAdaptiveProfileKind.High => 24,
            VideoAdaptiveProfileKind.Medium => 18,
            _ => 10,
        };
        var bitrate = kind switch
        {
            VideoAdaptiveProfileKind.High => 4_000_000,
            VideoAdaptiveProfileKind.Medium => 2_500_000,
            _ => 1_500_000,
        };
        return new VideoAdaptiveProfile(
            kind,
            width,
            height,
            framesPerSecond,
            bitrate);
    }

    private static int Align(double value) =>
        Math.Max(16, (int)Math.Round(value / 16d) * 16);
}
