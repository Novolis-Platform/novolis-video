namespace Novolis.Video.Edit;

/// <summary>Timed title / caption burned over the preview and export.</summary>
public sealed class TextOverlay
{
    public TextOverlay(
        Guid id,
        string text,
        TimeSpan timelineStart,
        TimeSpan duration,
        Rgba8? color = null,
        double anchorX = 0.5,
        double anchorY = 0.82)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentOutOfRangeException.ThrowIfNegative(timelineStart.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegative(anchorX);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anchorX, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(anchorY);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anchorY, 1);

        Id = id;
        Text = text;
        TimelineStart = timelineStart;
        Duration = duration;
        Color = color ?? new Rgba8(255, 240, 220);
        AnchorX = anchorX;
        AnchorY = anchorY;
    }

    public Guid Id { get; }
    public string Text { get; set; }
    public TimeSpan TimelineStart { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan TimelineEnd => TimelineStart + Duration;
    public Rgba8 Color { get; set; }
    public double AnchorX { get; set; }
    public double AnchorY { get; set; }

    public bool Contains(TimeSpan timelineTime) =>
        timelineTime >= TimelineStart && timelineTime < TimelineEnd;
}
