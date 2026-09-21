namespace Novolis.Video;

/// <summary>One raw captured frame with explicit ownership of its pixel buffer.</summary>
public sealed class RawVideoFrame
{
    /// <summary>Creates a raw video frame.</summary>
    public RawVideoFrame(
        int width,
        int height,
        int stride,
        VideoPixelFormat format,
        byte[] pixels,
        long timestamp)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, width);
        ArgumentNullException.ThrowIfNull(pixels);
        Width = width;
        Height = height;
        Stride = stride;
        Format = format;
        Pixels = pixels;
        Timestamp = timestamp;
    }

    /// <summary>Frame width in pixels.</summary>
    public int Width { get; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; }

    /// <summary>Bytes between adjacent rows.</summary>
    public int Stride { get; }

    /// <summary>Pixel layout.</summary>
    public VideoPixelFormat Format { get; }

    /// <summary>Pixel buffer owned by this frame.</summary>
    public byte[] Pixels { get; }

    /// <summary>Monotonic timestamp in 100-nanosecond units.</summary>
    public long Timestamp { get; }
}
