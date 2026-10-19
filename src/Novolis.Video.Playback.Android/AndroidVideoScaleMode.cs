namespace Novolis.Video.Playback.Android;

/// <summary>Native fit mode applied before gesture zoom.</summary>
public enum AndroidVideoScaleMode
{
    /// <summary>Keep the complete video visible.</summary>
    FitView,

    /// <summary>Scale the video to the view width.</summary>
    FitWidth,

    /// <summary>Scale the video to the view height.</summary>
    FitHeight,
}
