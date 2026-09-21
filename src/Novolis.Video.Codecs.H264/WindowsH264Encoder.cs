using SharpMediaFoundationInterop.Transforms.Colors;
using SharpMediaFoundationInterop.Transforms.H264;
using Windows.Win32;
using Novolis.Video;

namespace Novolis.Video.Codecs.H264;

/// <summary>Encodes BGRA desktop frames with the Windows Media Foundation H.264 MFT.</summary>
public sealed class WindowsH264Encoder : IVideoEncoder
{
    private readonly ColorConverter _colorConverter;
    private readonly H264Encoder _encoder;
    private byte[] _nv12Buffer;
    private byte[] _encodedBuffer;
    private bool _firstFrame = true;
    private bool _disposed;

    /// <summary>Creates an H.264 encoder for a fixed-size stream.</summary>
    public WindowsH264Encoder(
        int width,
        int height,
        int framesPerSecond = 30,
        int averageBitrate = 8_000_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(averageBitrate);

        _colorConverter = new ColorConverter(
            PInvoke.MFVideoFormat_RGB32,
            PInvoke.MFVideoFormat_NV12,
            (uint)width,
            (uint)height);
        _colorConverter.Initialize();

        _encoder = new H264Encoder(
            (uint)width,
            (uint)height,
            (uint)framesPerSecond,
            1,
            (uint)averageBitrate);
        _encoder.Initialize();
        _nv12Buffer = new byte[_colorConverter.OutputSize];
        _encodedBuffer = new byte[_encoder.OutputSize];
    }

    /// <inheritdoc />
    public EncodedVideoFrame Encode(RawVideoFrame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Format != VideoPixelFormat.Bgra32)
            throw new ArgumentException("The Windows H.264 encoder expects BGRA32 input.", nameof(frame));

        var tightPixels = CopyTightBgra(frame);
        if (!_colorConverter.ProcessInput(tightPixels, frame.Timestamp)
            || !_colorConverter.ProcessOutput(ref _nv12Buffer, out _))
        {
            throw new InvalidOperationException("Windows Media Foundation did not produce an NV12 frame.");
        }

        if (!_encoder.ProcessInput(_nv12Buffer, frame.Timestamp)
            || !_encoder.ProcessOutput(ref _encodedBuffer, out var length))
        {
            throw new InvalidOperationException("Windows Media Foundation did not produce an H.264 access unit.");
        }

        var accessUnit = _encodedBuffer.AsSpan(0, checked((int)length)).ToArray();
        var isKeyFrame = _firstFrame;
        _firstFrame = false;
        return new EncodedVideoFrame(
            frame.Width,
            frame.Height,
            frame.Timestamp,
            accessUnit,
            isKeyFrame);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _encoder.Dispose();
        _colorConverter.Dispose();
    }

    private static byte[] CopyTightBgra(RawVideoFrame frame)
    {
        var rowBytes = checked(frame.Width * 4);
        if (frame.Stride == rowBytes)
            return frame.Pixels;

        var tight = new byte[checked(rowBytes * frame.Height)];
        for (var row = 0; row < frame.Height; row++)
        {
            frame.Pixels.AsSpan(row * frame.Stride, rowBytes)
                .CopyTo(tight.AsSpan(row * rowBytes, rowBytes));
        }

        return tight;
    }
}
