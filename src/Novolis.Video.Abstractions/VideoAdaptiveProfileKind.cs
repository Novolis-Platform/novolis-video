namespace Novolis.Video;

/// <summary>Quality tier selected for an adaptive video stream.</summary>
public enum VideoAdaptiveProfileKind
{
    /// <summary>Highest quality for a short, low-loss path.</summary>
    High,

    /// <summary>Balanced quality for a longer routed path.</summary>
    Medium,

    /// <summary>Lowest quality under sustained pressure.</summary>
    Low,
}
