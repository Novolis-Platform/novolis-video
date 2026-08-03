using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>
/// Host-owned pixel source for image/video assets. Color cards do not need a provider.
/// </summary>
public interface IFrameProvider
{
    bool TryGetFrame(
        MediaAsset asset,
        TimeSpan sourceTime,
        int width,
        int height,
        out VideoFrame frame);
}
