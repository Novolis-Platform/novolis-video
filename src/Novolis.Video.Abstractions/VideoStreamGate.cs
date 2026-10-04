namespace Novolis.Video;

/// <summary>
/// Coordinates key-frame recovery and stale decoded-frame suppression.
/// </summary>
public sealed class VideoStreamGate
{
    private long _generation;
    private int _requiresKeyFrame;

    /// <summary>Gets the current stream generation.</summary>
    public long Generation => Interlocked.Read(ref _generation);

    /// <summary>
    /// Invalidates pending decoder work and requires the next accepted frame
    /// to be an intra frame.
    /// </summary>
    public void RequireKeyFrame()
    {
        Interlocked.Increment(ref _generation);
        Volatile.Write(ref _requiresKeyFrame, 1);
    }

    /// <summary>
    /// Accepts a frame when it can safely begin or continue the stream.
    /// </summary>
    public bool TryAccept(EncodedVideoFrame frame, out long generation)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return TryAccept(frame.IsKeyFrame, out generation);
    }

    /// <summary>
    /// Accepts a frame when it can safely begin or continue the stream.
    /// </summary>
    public bool TryAccept(bool isKeyFrame, out long generation)
    {
        generation = Generation;
        if (Volatile.Read(ref _requiresKeyFrame) != 0
            && !isKeyFrame)
        {
            return false;
        }

        if (isKeyFrame)
            Volatile.Write(ref _requiresKeyFrame, 0);
        return true;
    }

    /// <summary>Gets whether decoded work belongs to the current stream.</summary>
    public bool IsCurrent(long generation) =>
        generation == Generation;
}
