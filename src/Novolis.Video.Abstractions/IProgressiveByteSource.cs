namespace Novolis.Video;

/// <summary>
/// Provides random-access bytes while the underlying source is still arriving.
/// </summary>
/// <remarks>
/// A read must wait for unavailable bytes instead of returning zero-filled data.
/// This allows a media demuxer to distinguish a genuinely truncated source from
/// a source whose next P2P range has not arrived yet.
/// </remarks>
public interface IProgressiveByteSource : IAsyncDisposable
{
    /// <summary>
    /// Gets the total length, or <c>-1</c> when the source length is not known.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Gets whether the complete requested range is currently readable.
    /// </summary>
    bool IsRangeAvailable(long offset, int length);

    /// <summary>
    /// Hints that a consumer will need the specified range soon.
    /// </summary>
    void RequestRange(long offset, int length);

    /// <summary>
    /// Waits for the requested range and reads it.
    /// </summary>
    /// <returns>The number of bytes read, or zero at the end of the source.</returns>
    ValueTask<int> ReadAtAsync(
        long offset,
        Memory<byte> destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until the complete requested range is readable.
    /// </summary>
    ValueTask WaitForRangeAsync(
        long offset,
        int length,
        CancellationToken cancellationToken = default);
}
