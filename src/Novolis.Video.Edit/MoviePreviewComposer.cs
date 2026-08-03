using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Resolves the storyboard at a playhead and emits a preview <see cref="VideoFrame"/>.</summary>
public sealed class MoviePreviewComposer
{
    readonly IFrameProvider? _frames;

    public MoviePreviewComposer(IFrameProvider? frames = null)
    {
        _frames = frames;
    }

    public VideoFrame Compose(MovieProject project, TimeSpan position)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentOutOfRangeException.ThrowIfNegative(position.Ticks);

        var clip = StoryboardQuery.ClipAt(project, position);
        if (clip is null)
            return SolidColorFrames.Black(project.Width, project.Height);

        var asset = project.FindAsset(clip.AssetId);
        if (asset is null)
            return SolidColorFrames.Black(project.Width, project.Height);

        if (asset.Kind == MediaKind.Color && asset.Color is { } color)
            return SolidColorFrames.Create(project.Width, project.Height, color);

        if (asset.Kind == MediaKind.Audio)
            return SolidColorFrames.Black(project.Width, project.Height);

        if (_frames is not null
            && _frames.TryGetFrame(asset, clip.SourceTimeAt(position), project.Width, project.Height, out var frame))
            return frame;

        // Missing decoder / still: dim slate so the UI still shows "something is here".
        return SolidColorFrames.Create(project.Width, project.Height, new Rgba8(32, 32, 40));
    }
}
