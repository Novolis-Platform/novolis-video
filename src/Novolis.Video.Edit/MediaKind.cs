namespace Novolis.Video.Edit;

/// <summary>Imported collection item kind (Movie Maker collections pane).</summary>
public enum MediaKind
{
    /// <summary>Solid color still / title card (always previewable).</summary>
    Color,

    /// <summary>Still image file; needs an <see cref="IFrameProvider"/> for pixels.</summary>
    Image,

    /// <summary>Video file; needs an <see cref="IFrameProvider"/> (decode is host-owned).</summary>
    Video,

    /// <summary>Audio-only clip (no video plane).</summary>
    Audio,
}
