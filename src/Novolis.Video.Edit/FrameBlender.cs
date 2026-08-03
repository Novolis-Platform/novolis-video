using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Pixel blending helpers for transitions.</summary>
public static class FrameBlender
{
    /// <summary>Linear crossfade. <paramref name="amount"/> 0 = only A, 1 = only B.</summary>
    public static VideoFrame Fade(VideoFrame a, VideoFrame b, double amount)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Width != b.Width || a.Height != b.Height || a.Format != b.Format)
            throw new ArgumentException("Frames must match for fade.");

        amount = Math.Clamp(amount, 0, 1);
        var inv = 1 - amount;
        var pixels = new byte[a.Pixels.Length];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)Math.Clamp((int)(a.Pixels[i] * inv + b.Pixels[i] * amount + 0.5), 0, 255);

        return new VideoFrame(a.Width, a.Height, a.Stride, a.Format, pixels);
    }

    /// <summary>Horizontal wipe from left to right. <paramref name="amount"/> 0 = A, 1 = B.</summary>
    public static VideoFrame Wipe(VideoFrame a, VideoFrame b, double amount)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Width != b.Width || a.Height != b.Height || a.Format != b.Format)
            throw new ArgumentException("Frames must match for wipe.");

        amount = Math.Clamp(amount, 0, 1);
        var cut = (int)(a.Width * amount);
        var bpp = a.Format == VideoPixelFormat.Bgra32 ? 4 : 3;
        var pixels = new byte[a.Pixels.Length];
        for (var y = 0; y < a.Height; y++)
        {
            var row = y * a.Stride;
            var leftBytes = cut * bpp;
            Buffer.BlockCopy(b.Pixels, row, pixels, row, leftBytes);
            Buffer.BlockCopy(a.Pixels, row + leftBytes, pixels, row + leftBytes, a.Stride - leftBytes);
        }

        return new VideoFrame(a.Width, a.Height, a.Stride, a.Format, pixels);
    }
}
