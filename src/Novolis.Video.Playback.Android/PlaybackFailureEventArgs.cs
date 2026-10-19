namespace Novolis.Video.Playback.Android;

/// <summary>Describes a native playback failure without retaining media details.</summary>
public sealed class PlaybackFailureEventArgs : EventArgs
{
    /// <summary>Initializes playback failure details.</summary>
    public PlaybackFailureEventArgs(bool duringPreparation) =>
        DuringPreparation = duringPreparation;

    /// <summary>Gets whether preparation failed before the first frame.</summary>
    public bool DuringPreparation { get; }
}
