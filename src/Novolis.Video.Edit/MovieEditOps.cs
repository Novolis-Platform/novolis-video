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

        var right = new TimelineClip(Guid.NewGuid(), clip.AssetId, timelineTime, rightDuration, rightOffset);
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
}
