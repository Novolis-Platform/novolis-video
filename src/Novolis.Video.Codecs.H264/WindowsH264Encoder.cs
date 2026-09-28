using SharpMediaFoundationInterop.Transforms.Colors;
using SharpMediaFoundationInterop.Transforms.H264;
using Windows.Win32;
using Novolis.Video;

namespace Novolis.Video.Codecs.H264;

/// <summary>Encodes BGRA desktop frames with the Windows Media Foundation H.264 MFT.</summary>
public sealed class WindowsH264Encoder : IVideoEncoder
{
    private readonly int _encodedWidth;
    private readonly int _encodedHeight;
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

        _encodedWidth = RoundUpToCodecMultiple(width);
        _encodedHeight = RoundUpToCodecMultiple(height);
        _colorConverter = new ColorConverter(
            PInvoke.MFVideoFormat_RGB32,
            PInvoke.MFVideoFormat_NV12,
            (uint)_encodedWidth,
            (uint)_encodedHeight);
        _colorConverter.Initialize();

        _encoder = new H264Encoder(
            (uint)_encodedWidth,
            (uint)_encodedHeight,
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
        if (!TryEncode(frame, out var encoded))
        {
            throw new InvalidOperationException(
                "Windows Media Foundation accepted the frame but has not produced an H.264 access unit yet.");
        }

        return encoded
            ?? throw new InvalidOperationException(
                "Windows Media Foundation reported an encoded frame without an access unit.");
    }

    /// <summary>
    /// Submits a frame and reports whether Media Foundation has an access unit ready.
    /// Encoders are allowed to prime internally, so the first submitted frame may not
    /// produce output until a later frame is submitted.
    /// </summary>
    public bool TryEncode(
        RawVideoFrame frame,
        out EncodedVideoFrame? encoded)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Format != VideoPixelFormat.Bgra32)
            throw new ArgumentException("The Windows H.264 encoder expects BGRA32 input.", nameof(frame));

        var tightPixels = CopyPaddedBgra(frame, _encodedWidth, _encodedHeight);
        if (!_colorConverter.ProcessInput(tightPixels, frame.Timestamp)
            || !_colorConverter.ProcessOutput(ref _nv12Buffer, out _))
        {
            throw new InvalidOperationException("Windows Media Foundation did not produce an NV12 frame.");
        }

        if (!_encoder.ProcessInput(_nv12Buffer, frame.Timestamp))
        {
            throw new InvalidOperationException("Windows Media Foundation rejected the NV12 frame.");
        }

        while (_encoder.ProcessOutput(ref _encodedBuffer, out var length))
        {
            if (length == 0)
                continue;

            var accessUnit = _encodedBuffer.AsSpan(0, checked((int)length)).ToArray();
            var isKeyFrame = _firstFrame;
            _firstFrame = false;
            encoded = new EncodedVideoFrame(
                _encodedWidth,
                _encodedHeight,
                frame.Timestamp,
                accessUnit,
                isKeyFrame);
            return true;
        }

        encoded = null;
        return false;
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

    private static byte[] CopyPaddedBgra(
        RawVideoFrame frame,
        int width,
        int height)
    {
        var sourceRowBytes = checked(frame.Width * 4);
        var destinationRowBytes = checked(width * 4);
        if (frame.Width == width
            && frame.Height == height
            && frame.Stride == sourceRowBytes)
            return frame.Pixels;

        var padded = new byte[checked(destinationRowBytes * height)];
        var rows = Math.Min(frame.Height, height);
        var bytesToCopy = Math.Min(sourceRowBytes, destinationRowBytes);
        for (var row = 0; row < rows; row++)
        {
            frame.Pixels.AsSpan(row * frame.Stride, bytesToCopy)
                .CopyTo(padded.AsSpan(row * destinationRowBytes, bytesToCopy));
        }

        return padded;
    }

    private static int RoundUpToCodecMultiple(int value) =>
        checked((value + (int)H264Encoder.H264_RES_MULTIPLE - 1)
            / (int)H264Encoder.H264_RES_MULTIPLE
            * (int)H264Encoder.H264_RES_MULTIPLE);
}
