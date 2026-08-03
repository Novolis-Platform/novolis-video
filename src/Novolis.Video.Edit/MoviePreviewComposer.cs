using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Resolves the storyboard at a playhead and emits a preview <see cref="VideoFrame"/>.</summary>
public sealed class MoviePreviewComposer
{
    readonly IFrameProvider? _frames;
    readonly ITextOverlayRenderer? _text;

    public MoviePreviewComposer(IFrameProvider? frames = null, ITextOverlayRenderer? text = null)
    {
        _frames = frames;
        _text = text ?? new BitmapFontOverlay();
    }

    public VideoFrame Compose(MovieProject project, TimeSpan position)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentOutOfRangeException.ThrowIfNegative(position.Ticks);

        var frame = ComposeVideoPlane(project, position);
        var active = ActiveOverlays(project, position);
        if (active.Count > 0)
            _text?.Apply(frame, active);
        return frame;
    }

    VideoFrame ComposeVideoPlane(MovieProject project, TimeSpan position)
    {
        var clip = StoryboardQuery.ClipAt(project, position);
        if (clip is null)
            return SolidColorFrames.Black(project.Width, project.Height);

        var primary = RenderClip(project, clip, position);
        var next = NextClip(project, clip);
        if (next is null
            || clip.OutTransition == TransitionKind.None
            || clip.OutTransitionDuration <= TimeSpan.Zero)
            return primary;

        var transition = clip.OutTransitionDuration;
        if (transition > clip.Duration)
            transition = clip.Duration;
        var transitionStart = clip.TimelineEnd - transition;
        if (position < transitionStart)
            return primary;

        var amount = (position - transitionStart).TotalSeconds / transition.TotalSeconds;
        var incomingTime = next.TimelineStart + (position - transitionStart);
        if (incomingTime >= next.TimelineEnd)
            incomingTime = next.TimelineEnd - TimeSpan.FromTicks(1);
        var incoming = RenderClip(project, next, incomingTime);

        return clip.OutTransition switch
        {
            TransitionKind.Wipe => FrameBlender.Wipe(primary, incoming, amount),
            _ => FrameBlender.Fade(primary, incoming, amount),
        };
    }

    VideoFrame RenderClip(MovieProject project, TimelineClip clip, TimeSpan position)
    {
        var asset = project.FindAsset(clip.AssetId);
        if (asset is null)
            return SolidColorFrames.Black(project.Width, project.Height);

        if (asset.Kind == MediaKind.Color && asset.Color is { } color)
            return SolidColorFrames.Create(project.Width, project.Height, color);

        if (asset.Kind == MediaKind.Audio)
            return SolidColorFrames.Black(project.Width, project.Height);

        if (_frames is not null
            && _frames.TryGetFrame(asset, clip.SourceTimeAt(position), project.Width, project.Height, out var frame))
            return frame.Clone();

        return SolidColorFrames.Create(project.Width, project.Height, new Rgba8(32, 32, 40));
    }

    static TimelineClip? NextClip(MovieProject project, TimelineClip clip)
    {
        var index = -1;
        for (var i = 0; i < project.Clips.Count; i++)
        {
            if (project.Clips[i].Id == clip.Id)
            {
                index = i;
                break;
            }
        }

        if (index < 0 || index + 1 >= project.Clips.Count)
            return null;
        return project.Clips[index + 1];
    }

    static List<TextOverlay> ActiveOverlays(MovieProject project, TimeSpan position)
    {
        var list = new List<TextOverlay>();
        foreach (var overlay in project.TextOverlays)
        {
            if (overlay.Contains(position))
                list.Add(overlay);
        }

        return list;
    }
}
