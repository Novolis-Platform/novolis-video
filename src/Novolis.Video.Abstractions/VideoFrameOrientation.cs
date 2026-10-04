namespace Novolis.Video;

/// <summary>Normalizes decoded BGRA frame row orientation.</summary>
public static class VideoFrameOrientation
{
    /// <summary>
    /// Flips a tightly packed BGRA frame vertically in place when a decoder
    /// exposes rows in the inverse order used by <see cref="RawVideoFrame"/>.
    /// </summary>
    public static void NormalizeTopToBottom(
        Span<byte> pixels,
        int width,
        int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
        var stride = checked(width * 4);
        if (pixels.Length < checked(stride * height))
        {
            throw new ArgumentException(
                "The BGRA buffer is smaller than the requested frame.",
                nameof(pixels));
        }

        var row = new byte[stride];
        for (var top = 0; top < height / 2; top++)
        {
            var bottom = height - top - 1;
            pixels.Slice(top * stride, stride).CopyTo(row.AsSpan());
            pixels.Slice(bottom * stride, stride)
                .CopyTo(pixels.Slice(top * stride, stride));
            row.AsSpan().CopyTo(pixels.Slice(bottom * stride, stride));
        }
    }
}
