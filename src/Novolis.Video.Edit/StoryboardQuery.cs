namespace Novolis.Video.Edit;

/// <summary>Read-only queries over a storyboard.</summary>
public static class StoryboardQuery
{
    public static TimeSpan TotalDuration(MovieProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var end = TimeSpan.Zero;
        foreach (var clip in project.Clips)
        {
            if (clip.TimelineEnd > end)
                end = clip.TimelineEnd;
        }

        return end;
    }

    public static TimelineClip? ClipAt(MovieProject project, TimeSpan timelineTime)
    {
        ArgumentNullException.ThrowIfNull(project);
        foreach (var clip in project.Clips)
        {
            if (clip.Contains(timelineTime))
                return clip;
        }

        return null;
    }

    public static MediaAsset? AssetAt(MovieProject project, TimeSpan timelineTime)
    {
        var clip = ClipAt(project, timelineTime);
        return clip is null ? null : project.FindAsset(clip.AssetId);
    }
}
