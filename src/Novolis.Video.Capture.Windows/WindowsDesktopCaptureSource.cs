using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Novolis.Video;

namespace Novolis.Video.Capture.Windows;

/// <summary>Captures the visible Windows monitor topology as BGRA frames.</summary>
public sealed class WindowsDesktopCaptureSource : IVideoCaptureSource
{
    private readonly TimeSpan _frameInterval;
    private readonly bool _captureAllMonitors;
    private CancellationTokenSource? _cancellation;
    private Task? _captureTask;

    /// <summary>Creates a monitor capture source.</summary>
    public WindowsDesktopCaptureSource(int framesPerSecond = 30, bool captureAllMonitors = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(framesPerSecond, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(framesPerSecond, 120);
        _frameInterval = TimeSpan.FromSeconds(1d / framesPerSecond);
        _captureAllMonitors = captureAllMonitors;
    }

    /// <inheritdoc />
    public event Action<RawVideoFrame>? FrameCaptured;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_captureTask is not null)
            return Task.CompletedTask;

        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _captureTask = Task.Run(() => CaptureLoopAsync(_cancellation.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var task = Interlocked.Exchange(ref _captureTask, null);
        var cancellation = Interlocked.Exchange(ref _cancellation, null);
        if (task is null || cancellation is null)
            return;

        cancellation.Cancel();
        try
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_frameInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var frame = CaptureFrame();
                if (frame is not null)
                    FrameCaptured?.Invoke(frame);
            }
            catch (ExternalException)
            {
                // The interactive session may be transitioning between lock and unlock.
            }
        }
    }

    private RawVideoFrame? CaptureFrame()
    {
        var bounds = GetCaptureBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                bounds.Left,
                bounds.Top,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var locked = bitmap.LockBits(
            rectangle,
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(locked.Stride);
            var pixels = new byte[stride * bitmap.Height];
            for (var row = 0; row < bitmap.Height; row++)
            {
                var sourceRow = locked.Stride < 0
                    ? bitmap.Height - row - 1
                    : row;
                var source = IntPtr.Add(locked.Scan0, sourceRow * locked.Stride);
                Marshal.Copy(source, pixels, row * stride, stride);
            }

            return new RawVideoFrame(
                bitmap.Width,
                bitmap.Height,
                stride,
                VideoPixelFormat.Bgra32,
                pixels,
                DateTime.UtcNow.Ticks);
        }
        finally
        {
            bitmap.UnlockBits(locked);
        }
    }

    private Rectangle GetCaptureBounds()
    {
        if (!_captureAllMonitors)
            return Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

        var screens = Screen.AllScreens;
        if (screens.Length == 0)
            return Rectangle.Empty;

        var bounds = screens[0].Bounds;
        for (var index = 1; index < screens.Length; index++)
            bounds = Rectangle.Union(bounds, screens[index].Bounds);
        return bounds;
    }
}
