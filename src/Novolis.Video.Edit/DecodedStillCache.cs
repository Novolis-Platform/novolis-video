using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Simple still-image cache implementing <see cref="IFrameProvider"/>.</summary>
public sealed class DecodedStillCache : IFrameProvider
{
    readonly Dictionary<Guid, VideoFrame> _stills = [];

    public void SetStill(Guid assetId, VideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _stills[assetId] = frame;
    }

    public bool Remove(Guid assetId) => _stills.Remove(assetId);

    public bool TryGetFrame(
        MediaAsset asset,
        TimeSpan sourceTime,
        int width,
        int height,
        out VideoFrame frame)
    {
        ArgumentNullException.ThrowIfNull(asset);
        _ = sourceTime;
        _ = width;
        _ = height;
        return _stills.TryGetValue(asset.Id, out frame!);
    }
}
