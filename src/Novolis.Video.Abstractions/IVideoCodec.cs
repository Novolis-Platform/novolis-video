namespace Novolis.Video;

/// <summary>Encodes raw frames for a media transport.</summary>
public interface IVideoEncoder : IDisposable
{
    /// <summary>Encodes one raw frame.</summary>
    EncodedVideoFrame Encode(RawVideoFrame frame);
}
