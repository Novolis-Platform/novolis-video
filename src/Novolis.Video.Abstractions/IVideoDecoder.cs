namespace Novolis.Video;

/// <summary>Decodes encoded access units for presentation.</summary>
public interface IVideoDecoder : IDisposable
{
    /// <summary>Decodes one encoded frame.</summary>
    RawVideoFrame Decode(EncodedVideoFrame frame);
}
