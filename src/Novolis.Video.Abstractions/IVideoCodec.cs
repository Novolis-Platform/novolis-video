namespace Novolis.Video;

/// <summary>Encodes raw frames for a media transport.</summary>
public interface IVideoEncoder : IDisposable
{
    /// <summary>Encodes one raw frame.</summary>
    EncodedVideoFrame Encode(RawVideoFrame frame);
}

/// <summary>Decodes encoded access units for presentation.</summary>
public interface IVideoDecoder : IDisposable
{
    /// <summary>Decodes one encoded frame.</summary>
    RawVideoFrame Decode(EncodedVideoFrame frame);
}
