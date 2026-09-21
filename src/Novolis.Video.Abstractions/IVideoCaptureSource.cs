namespace Novolis.Video;

/// <summary>Produces raw frames from a platform capture source.</summary>
public interface IVideoCaptureSource : IAsyncDisposable
{
    /// <summary>Raised when a frame is captured.</summary>
    event Action<RawVideoFrame>? FrameCaptured;

    /// <summary>Starts capture.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops capture.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
