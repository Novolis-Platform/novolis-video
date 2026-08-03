using System.Buffers.Binary;
using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>24-bit BMP IO for demo stills and frame-sequence export (no third-party deps).</summary>
public static class BmpFile
{
    public static void WriteBgraFrame(string path, VideoFrame frame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Format != VideoPixelFormat.Bgra32)
            throw new ArgumentException("Only BGRA32 frames supported.", nameof(frame));

        var rowSize = ((frame.Width * 3 + 3) / 4) * 4;
        var pixelBytes = rowSize * frame.Height;
        var fileSize = 54 + pixelBytes;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        using var fs = File.Create(path);
        Span<byte> header = stackalloc byte[54];
        header[0] = (byte)'B';
        header[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(header[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(header[10..], 54);
        BinaryPrimitives.WriteInt32LittleEndian(header[14..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(header[18..], frame.Width);
        BinaryPrimitives.WriteInt32LittleEndian(header[22..], frame.Height);
        BinaryPrimitives.WriteInt16LittleEndian(header[26..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(header[28..], 24);
        BinaryPrimitives.WriteInt32LittleEndian(header[34..], pixelBytes);
        fs.Write(header);

        var row = new byte[rowSize];
        for (var y = frame.Height - 1; y >= 0; y--)
        {
            Array.Clear(row);
            var src = y * frame.Stride;
            for (var x = 0; x < frame.Width; x++)
            {
                var si = src + x * 4;
                var di = x * 3;
                row[di] = frame.Pixels[si];
                row[di + 1] = frame.Pixels[si + 1];
                row[di + 2] = frame.Pixels[si + 2];
            }

            fs.Write(row);
        }
    }

    public static VideoFrame ReadToBgra(string path, int? targetWidth = null, int? targetHeight = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 54 || bytes[0] != 'B' || bytes[1] != 'M')
            throw new InvalidDataException("Not a BMP file.");

        var offset = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(10));
        var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(18));
        var height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(22)));
        var bpp = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(28));
        if (bpp != 24)
            throw new InvalidDataException("Only 24-bit BMP supported.");

        var rowSize = ((width * 3 + 3) / 4) * 4;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var srcY = height - 1 - y;
            var src = offset + srcY * rowSize;
            var dst = y * stride;
            for (var x = 0; x < width; x++)
            {
                var si = src + x * 3;
                var di = dst + x * 4;
                pixels[di] = bytes[si];
                pixels[di + 1] = bytes[si + 1];
                pixels[di + 2] = bytes[si + 2];
                pixels[di + 3] = 255;
            }
        }

        var frame = new VideoFrame(width, height, stride, VideoPixelFormat.Bgra32, pixels);
        if (targetWidth is int tw && targetHeight is int th && (tw != width || th != height))
            return ScaleNearest(frame, tw, th);
        return frame;
    }

    public static VideoFrame ScaleNearest(VideoFrame source, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(source);
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var sy = y * source.Height / height;
            for (var x = 0; x < width; x++)
            {
                var sx = x * source.Width / width;
                var si = sy * source.Stride + sx * 4;
                var di = y * stride + x * 4;
                pixels[di] = source.Pixels[si];
                pixels[di + 1] = source.Pixels[si + 1];
                pixels[di + 2] = source.Pixels[si + 2];
                pixels[di + 3] = source.Pixels[si + 3];
            }
        }

        return new VideoFrame(width, height, stride, VideoPixelFormat.Bgra32, pixels);
    }

    /// <summary>Creates a simple gradient still useful for demos.</summary>
    public static VideoFrame CreateGradient(int width, int height, Rgba8 topLeft, Rgba8 bottomRight)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var v = height <= 1 ? 0 : y / (double)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var u = width <= 1 ? 0 : x / (double)(width - 1);
                var t = (u + v) * 0.5;
                var i = y * stride + x * 4;
                pixels[i] = Lerp(topLeft.B, bottomRight.B, t);
                pixels[i + 1] = Lerp(topLeft.G, bottomRight.G, t);
                pixels[i + 2] = Lerp(topLeft.R, bottomRight.R, t);
                pixels[i + 3] = 255;
            }
        }

        return new VideoFrame(width, height, stride, VideoPixelFormat.Bgra32, pixels);
    }

    static byte Lerp(byte a, byte b, double t) =>
        (byte)Math.Clamp((int)(a + (b - a) * t + 0.5), 0, 255);
}
