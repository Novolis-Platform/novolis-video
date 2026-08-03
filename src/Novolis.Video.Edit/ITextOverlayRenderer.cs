using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Host-owned text burn-in (Avalonia or bitmap font).</summary>
public interface ITextOverlayRenderer
{
    void Apply(VideoFrame frame, IReadOnlyList<TextOverlay> overlays);
}
