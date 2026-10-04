namespace Novolis.Video;

/// <summary>Describes a fitted and transformed video rectangle.</summary>
public readonly record struct VideoFit(
    double Scale,
    double Zoom,
    double OriginX,
    double OriginY,
    double RenderedWidth,
    double RenderedHeight,
    double PanX,
    double PanY);
