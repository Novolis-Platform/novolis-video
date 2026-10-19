using Android.Media;
using Novolis.Video;

namespace Novolis.Video.Playback.Android;

/// <summary>
/// Bridges a progressive random-access source to Android's media framework.
/// </summary>
/// <remarks>
/// <see cref="MediaDataSource.ReadAt" /> is synchronous by contract, while a
/// P2P source may need to wait for a piece. The wait happens on the media
/// framework's data thread, never on the Android UI thread.
/// </remarks>
public sealed class ProgressiveMediaDataSource : MediaDataSource
{
    private readonly IProgressiveByteSource _source;
    private readonly bool _leaveSourceOpen;
    private readonly CancellationTokenSource _closeCancellation = new();
    private bool _closed;

    /// <summary>
    /// Initializes a data source adapter.
    /// </summary>
    /// <param name="source">The progressive byte source.</param>
    /// <param name="readTimeout">
    /// Maximum time to wait for one requested range. Use
    /// <see cref="Timeout.InfiniteTimeSpan" /> to wait without a deadline.
    /// </param>
    /// <param name="leaveSourceOpen">
    /// Keeps the source alive when Android closes this adapter.
    /// </param>
    public ProgressiveMediaDataSource(
        IProgressiveByteSource source,
        TimeSpan? readTimeout = null,
        bool leaveSourceOpen = false)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        ReadTimeout = readTimeout ?? TimeSpan.FromSeconds(45);
        if (ReadTimeout != Timeout.InfiniteTimeSpan && ReadTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(readTimeout));

        _leaveSourceOpen = leaveSourceOpen;
    }

    /// <summary>
    /// Gets the maximum time spent waiting for one unavailable range.
    /// </summary>
    public TimeSpan ReadTimeout { get; }

    /// <inheritdoc />
    public override long Size => _source.Length;

    /// <inheritdoc />
    public override int ReadAt(long position, byte[]? buffer, int offset, int size)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position));
        if (offset < 0 || offset > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (size < 0 || size > buffer.Length - offset)
            throw new ArgumentOutOfRangeException(nameof(size));
        if (size == 0 || _closed)
            return 0;
        if (_source.Length >= 0 && position >= _source.Length)
            return 0;

        _source.RequestRange(position, size);
        using var timeout = ReadTimeout == Timeout.InfiniteTimeSpan
            ? null
            : new CancellationTokenSource(ReadTimeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _closeCancellation.Token,
            timeout?.Token ?? CancellationToken.None);

        return _source.ReadAtAsync(
                position,
                buffer!.AsMemory(offset, size),
                cancellation.Token)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc />
    public override void Close()
    {
        if (_closed)
            return;

        _closed = true;
        _closeCancellation.Cancel();
        _closeCancellation.Dispose();
        if (!_leaveSourceOpen)
            _source.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
