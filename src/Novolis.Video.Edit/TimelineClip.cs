namespace Novolis.Video.Edit;

/// <summary>One storyboard / timeline placement of a <see cref="MediaAsset"/>.</summary>
public sealed class TimelineClip
{
    public TimelineClip(
        Guid id,
        Guid assetId,
        TimeSpan timelineStart,
        TimeSpan duration,
        TimeSpan sourceOffset = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timelineStart.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceOffset.Ticks);
        Id = id;
        AssetId = assetId;
        TimelineStart = timelineStart;
        Duration = duration;
        SourceOffset = sourceOffset;
    }

    public Guid Id { get; }
    public Guid AssetId { get; }
    public TimeSpan TimelineStart { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan SourceOffset { get; set; }
    public TimeSpan TimelineEnd => TimelineStart + Duration;

    /// <summary>Transition played over the last <see cref="OutTransitionDuration"/> into the next clip.</summary>
    public TransitionKind OutTransition { get; set; } = TransitionKind.None;

    /// <summary>Length of the outgoing transition (clamped to clip duration at compose time).</summary>
    public TimeSpan OutTransitionDuration { get; set; }

    public bool Contains(TimeSpan timelineTime) =>
        timelineTime >= TimelineStart && timelineTime < TimelineEnd;

    public TimeSpan SourceTimeAt(TimeSpan timelineTime) =>
        SourceOffset + (timelineTime - TimelineStart);
}
