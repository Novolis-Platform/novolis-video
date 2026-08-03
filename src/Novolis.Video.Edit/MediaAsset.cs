namespace Novolis.Video.Edit;

/// <summary>One item in the project collections pane.</summary>
public sealed class MediaAsset
{
    public MediaAsset(
        Guid id,
        string name,
        MediaKind kind,
        TimeSpan duration,
        string? path = null,
        Rgba8? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        if (kind == MediaKind.Color && color is null)
            throw new ArgumentException("Color assets require a color.", nameof(color));
        if ((kind == MediaKind.Image || kind == MediaKind.Video || kind == MediaKind.Audio)
            && string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File assets require a path.", nameof(path));

        Id = id;
        Name = name;
        Kind = kind;
        Duration = duration;
        Path = path;
        Color = color;
    }

    public Guid Id { get; }
    public string Name { get; set; }
    public MediaKind Kind { get; }
    public TimeSpan Duration { get; set; }
    public string? Path { get; }
    public Rgba8? Color { get; }
}
