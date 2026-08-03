namespace Novolis.Video.Edit;

/// <summary>In-memory Movie Maker project: collections, storyboard, audio track, text overlays.</summary>
public sealed class MovieProject
{
    readonly List<MediaAsset> _assets = [];
    readonly List<TimelineClip> _clips = [];
    readonly List<TimelineClip> _audioClips = [];
    readonly List<TextOverlay> _textOverlays = [];

    public MovieProject(string title = "Untitled", int width = 640, int height = 480)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Title = title;
        Width = width;
        Height = height;
    }

    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public IReadOnlyList<MediaAsset> Assets => _assets;
    public IReadOnlyList<TimelineClip> Clips => _clips;
    public IReadOnlyList<TimelineClip> AudioClips => _audioClips;
    public IReadOnlyList<TextOverlay> TextOverlays => _textOverlays;

    internal List<MediaAsset> MutableAssets => _assets;
    internal List<TimelineClip> MutableClips => _clips;
    internal List<TimelineClip> MutableAudioClips => _audioClips;
    internal List<TextOverlay> MutableTextOverlays => _textOverlays;

    public MediaAsset? FindAsset(Guid id)
    {
        foreach (var asset in _assets)
        {
            if (asset.Id == id)
                return asset;
        }

        return null;
    }

    public TimelineClip? FindClip(Guid id)
    {
        foreach (var clip in _clips)
        {
            if (clip.Id == id)
                return clip;
        }

        return null;
    }
}
