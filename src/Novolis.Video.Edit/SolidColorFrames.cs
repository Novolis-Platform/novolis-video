using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Builds BGRA32 <see cref="VideoFrame"/> stills from <see cref="Rgba8"/>.</summary>
public static class SolidColorFrames
{
    public static VideoFrame Create(int width, int height, Rgba8 color)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = color.A;
        }

        return new VideoFrame(width, height, stride, VideoPixelFormat.Bgra32, pixels);
    }

    public static VideoFrame Black(int width, int height) =>
        Create(width, height, new Rgba8(0, 0, 0));
}
