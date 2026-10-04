namespace Novolis.Video;

/// <summary>
/// Applies bounded quality changes with cooldowns so network pressure does not
/// cause visible profile oscillation.
/// </summary>
public sealed class VideoAdaptiveProfileController
{
    private readonly int _sourceWidth;
    private readonly int _sourceHeight;
    private VideoAdaptiveProfile _current;
    private DateTimeOffset _lastChange;
    private long _lastDroppedFrames;
    private DateTimeOffset _stableSince;

    /// <summary>Creates a profile controller for one source size.</summary>
    public VideoAdaptiveProfileController(
        int sourceWidth,
        int sourceHeight,
        VideoAdaptiveProfileKind initialKind)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceHeight);
        _sourceWidth = sourceWidth;
        _sourceHeight = sourceHeight;
        _current = VideoAdaptiveProfile.ForSource(
            sourceWidth,
            sourceHeight,
            initialKind);
        _lastChange = DateTimeOffset.UtcNow;
        _stableSince = _lastChange;
    }

    /// <summary>Gets the profile currently applied to the stream.</summary>
    public VideoAdaptiveProfile Current => _current;

    /// <summary>
    /// Returns a new profile only when pressure or sustained stability warrants
    /// a change.
    /// </summary>
    public VideoAdaptiveProfile? Observe(
        long droppedFrames,
        long receivedFrames,
        double? frameAgeP95Milliseconds,
        double? inputRoundTripP95Milliseconds,
        DateTimeOffset now)
    {
        var dropped = droppedFrames > _lastDroppedFrames;
        _lastDroppedFrames = droppedFrames;
        var pressure = dropped
            || frameAgeP95Milliseconds is > 180
            || inputRoundTripP95Milliseconds is > 180;
        if (pressure)
        {
            _stableSince = now;
            if (now - _lastChange < TimeSpan.FromSeconds(4))
                return null;

            var nextKind = _current.Kind switch
            {
                VideoAdaptiveProfileKind.High => VideoAdaptiveProfileKind.Medium,
                VideoAdaptiveProfileKind.Medium => VideoAdaptiveProfileKind.Low,
                _ => VideoAdaptiveProfileKind.Low,
            };
            if (nextKind == _current.Kind)
                return null;

            return ChangeTo(nextKind, now);
        }

        if (receivedFrames == 0)
            return null;
        if (now - _stableSince < TimeSpan.FromSeconds(10)
            || now - _lastChange < TimeSpan.FromSeconds(6))
        {
            return null;
        }

        var upgradedKind = _current.Kind switch
        {
            VideoAdaptiveProfileKind.Low => VideoAdaptiveProfileKind.Medium,
            VideoAdaptiveProfileKind.Medium => VideoAdaptiveProfileKind.High,
            _ => VideoAdaptiveProfileKind.High,
        };
        if (upgradedKind == _current.Kind)
            return null;

        return ChangeTo(upgradedKind, now);
    }

    private VideoAdaptiveProfile ChangeTo(
        VideoAdaptiveProfileKind kind,
        DateTimeOffset now)
    {
        _current = VideoAdaptiveProfile.ForSource(
            _sourceWidth,
            _sourceHeight,
            kind);
        _lastChange = now;
        _stableSince = now;
        return _current;
    }
}
