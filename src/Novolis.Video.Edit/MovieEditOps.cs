namespace Novolis.Video.Edit;

/// <summary>Mutating storyboard / collection operations (Movie Maker task pane basics).</summary>
public static class MovieEditOps
{
    public static MediaAsset AddColorCard(
        MovieProject project,
        string name,
        Rgba8 color,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = new MediaAsset(Guid.NewGuid(), name, MediaKind.Color, duration, color: color);
        project.MutableAssets.Add(asset);
        return asset;
    }

    public static MediaAsset AddImage(
        MovieProject project,
        string name,
        string path,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = new MediaAsset(Guid.NewGuid(), name, MediaKind.Image, duration, path);
        project.MutableAssets.Add(asset);
        return asset;
    }

    public static MediaAsset AddVideo(
        MovieProject project,
        string name,
        string path,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = new MediaAsset(Guid.NewGuid(), name, MediaKind.Video, duration, path);
        project.MutableAssets.Add(asset);
        return asset;
    }

    /// <summary>Creates a sine-tone WAV on disk and adds it as an audio asset.</summary>
    public static MediaAsset AddToneAudio(
        MovieProject project,
        string name,
        string wavPath,
        double frequencyHz,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(project);
        ToneAudio.WriteSineWav(wavPath, frequencyHz, duration);
        var asset = new MediaAsset(Guid.NewGuid(), name, MediaKind.Audio, duration, wavPath);
        project.MutableAssets.Add(asset);
        return asset;
    }

    public static TimelineClip AppendToStoryboard(MovieProject project, MediaAsset asset, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(asset);
        if (project.FindAsset(asset.Id) is null)
            throw new InvalidOperationException("Asset is not in this project.");

        var start = StoryboardQuery.TotalDuration(project);
        var clipDuration = duration ?? asset.Duration;
        var clip = new TimelineClip(Guid.NewGuid(), asset.Id, start, clipDuration);
        project.MutableClips.Add(clip);
        return clip;
    }

    /// <summary>Places an audio asset on the audio track (starts at current audio end by default).</summary>
    public static TimelineClip AppendAudio(MovieProject project, MediaAsset asset, TimeSpan? start = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.Kind != MediaKind.Audio)
            throw new ArgumentException("Asset must be audio.", nameof(asset));
        if (project.FindAsset(asset.Id) is null)
            throw new InvalidOperationException("Asset is not in this project.");

        var t = start ?? AudioTrackEnd(project);
        var clip = new TimelineClip(Guid.NewGuid(), asset.Id, t, asset.Duration);
        project.MutableAudioClips.Add(clip);
        return clip;
    }

    public static TextOverlay AddTextOverlay(
        MovieProject project,
        string text,
        TimeSpan start,
        TimeSpan duration,
        Rgba8? color = null,
        double anchorX = 0.5,
        double anchorY = 0.82)
    {
        ArgumentNullException.ThrowIfNull(project);
        var overlay = new TextOverlay(Guid.NewGuid(), text, start, duration, color, anchorX, anchorY);
        project.MutableTextOverlays.Add(overlay);
        return overlay;
    }

    public static void SetOutTransition(
        TimelineClip clip,
        TransitionKind kind,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentOutOfRangeException.ThrowIfNegative(duration.Ticks);
        clip.OutTransition = kind;
        clip.OutTransitionDuration = duration;
    }

    public static bool RemoveClip(MovieProject project, Guid clipId, bool compact = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        var removed = project.MutableClips.RemoveAll(c => c.Id == clipId) > 0;
        if (removed && compact)
            CompactStoryboard(project);
        return removed;
    }

    /// <summary>Packs clips back-to-back from t=0 (classic storyboard, no gaps).</summary>
    public static void CompactStoryboard(MovieProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var t = TimeSpan.Zero;
        foreach (var clip in project.MutableClips)
        {
            clip.TimelineStart = t;
            t += clip.Duration;
        }
    }

    /// <summary>Splits the clip under <paramref name="timelineTime"/>; returns the right half, or null.</summary>
    public static TimelineClip? SplitAt(MovieProject project, TimeSpan timelineTime)
    {
        ArgumentNullException.ThrowIfNull(project);
        var clip = StoryboardQuery.ClipAt(project, timelineTime);
        if (clip is null)
            return null;

        var leftDuration = timelineTime - clip.TimelineStart;
        if (leftDuration <= TimeSpan.Zero || leftDuration >= clip.Duration)
            return null;

        var rightDuration = clip.Duration - leftDuration;
        var rightOffset = clip.SourceOffset + leftDuration;
        clip.Duration = leftDuration;

        var right = new TimelineClip(Guid.NewGuid(), clip.AssetId, timelineTime, rightDuration, rightOffset)
        {
            OutTransition = clip.OutTransition,
            OutTransitionDuration = clip.OutTransitionDuration,
        };
        clip.OutTransition = TransitionKind.None;
        clip.OutTransitionDuration = TimeSpan.Zero;

        var index = project.MutableClips.IndexOf(clip);
        project.MutableClips.Insert(index + 1, right);
        return right;
    }

    public static bool TrimClip(MovieProject project, Guid clipId, TimeSpan newDuration, bool compact = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newDuration.Ticks);
        var clip = project.FindClip(clipId);
        if (clip is null)
            return false;

        var asset = project.FindAsset(clip.AssetId);
        if (asset is not null && newDuration > asset.Duration - clip.SourceOffset)
            newDuration = asset.Duration - clip.SourceOffset;
        if (newDuration <= TimeSpan.Zero)
            return false;

        clip.Duration = newDuration;
        if (compact)
            CompactStoryboard(project);
        return true;
    }

    static TimeSpan AudioTrackEnd(MovieProject project)
    {
        var end = TimeSpan.Zero;
        foreach (var clip in project.AudioClips)
        {
            if (clip.TimelineEnd > end)
                end = clip.TimelineEnd;
        }

        return end;
    }
}
