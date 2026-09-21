using SharpMediaFoundationInterop.Transforms.Colors;
using SharpMediaFoundationInterop.Transforms.H264;
using Windows.Win32;
using Novolis.Video;

namespace Novolis.Video.Codecs.H264;

/// <summary>Decodes H.264 access units with the Windows Media Foundation decoder.</summary>
public sealed class WindowsH264Decoder : IVideoDecoder
{
    private readonly H264Decoder _decoder;
    private readonly ColorConverter _colorConverter;
    private byte[] _nv12Buffer;
    private byte[] _bgraBuffer;
    private bool _disposed;

    /// <summary>Creates a low-latency decoder for a fixed-size stream.</summary>
    public WindowsH264Decoder(int width, int height, int framesPerSecond = 30)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);

        _decoder = new H264Decoder(
            (uint)width,
            (uint)height,
            (uint)framesPerSecond,
            1,
            isLowLatency: true);
        _decoder.Initialize();
        _colorConverter = new ColorConverter(
            PInvoke.MFVideoFormat_NV12,
            PInvoke.MFVideoFormat_RGB32,
            (uint)width,
            (uint)height);
        _colorConverter.Initialize();
        _nv12Buffer = new byte[_decoder.OutputSize];
        _bgraBuffer = new byte[_colorConverter.OutputSize];
    }

    /// <inheritdoc />
    public RawVideoFrame Decode(EncodedVideoFrame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame);
        if (!_decoder.ProcessInput(frame.AccessUnit, frame.Timestamp))
            throw new InvalidOperationException("Windows Media Foundation rejected the H.264 access unit.");

        if (!_decoder.ProcessOutput(ref _nv12Buffer, out _)
            || !_colorConverter.ProcessInput(_nv12Buffer, frame.Timestamp)
            || !_colorConverter.ProcessOutput(ref _bgraBuffer, out _))
        {
            throw new InvalidOperationException("Windows Media Foundation did not produce a BGRA frame.");
        }

        return new RawVideoFrame(
            frame.Width,
            frame.Height,
            checked(frame.Width * 4),
            VideoPixelFormat.Bgra32,
            _bgraBuffer.ToArray(),
            frame.Timestamp);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _decoder.Dispose();
        _colorConverter.Dispose();
    }
}
