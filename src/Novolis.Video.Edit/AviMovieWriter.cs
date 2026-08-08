using System.Buffers.Binary;
using System.Text;
using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Writes an uncompressed BGR24 AVI with optional mono PCM audio (playable in WMP/VLC).</summary>
public static class AviMovieWriter
{
    /// <summary>Writes <paramref name="frames"/> and optional <paramref name="pcmMono16"/> to <paramref name="path"/>.</summary>
    public static void Write(
        string path,
        int width,
        int height,
        double framesPerSecond,
        IReadOnlyList<VideoFrame> frames,
        short[]? pcmMono16 = null,
        int audioSampleRate = ToneAudio.SampleRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
            throw new ArgumentException("At least one frame is required.", nameof(frames));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        var fps = Math.Max(1, (int)Math.Round(framesPerSecond));
        var hasAudio = pcmMono16 is { Length: > 0 };
        var rowSize = ((width * 3 + 3) / 4) * 4;
        var frameBytes = rowSize * height;
        var microSecPerFrame = (int)Math.Round(1_000_000.0 / fps);

        using var ms = new MemoryStream();
        WriteFourCC(ms, "RIFF");
        WriteInt32(ms, 0);
        WriteFourCC(ms, "AVI ");

        WriteFourCC(ms, "LIST");
        var hdrlSizePos = ms.Position;
        WriteInt32(ms, 0);
        WriteFourCC(ms, "hdrl");

        WriteAvih(ms, width, height, microSecPerFrame, frames.Count, frameBytes * fps, hasAudio ? 2 : 1);

        WriteFourCC(ms, "LIST");
        var strlVideoSizePos = ms.Position;
        WriteInt32(ms, 0);
        WriteFourCC(ms, "strl");
        WriteStrhVideo(ms, width, height, fps, frames.Count, frameBytes);
        WriteStrfVideo(ms, width, height, frameBytes);
        PatchChunkSize(ms, strlVideoSizePos);

        if (hasAudio)
        {
            WriteFourCC(ms, "LIST");
            var strlAudioSizePos = ms.Position;
            WriteInt32(ms, 0);
            WriteFourCC(ms, "strl");
            WriteStrhAudio(ms, audioSampleRate, pcmMono16!.Length);
            WriteStrfAudio(ms, audioSampleRate);
            PatchChunkSize(ms, strlAudioSizePos);
        }

        PatchChunkSize(ms, hdrlSizePos);

        WriteFourCC(ms, "LIST");
        var moviSizePos = ms.Position;
        WriteInt32(ms, 0);
        WriteFourCC(ms, "movi");

        var index = new List<IndexEntry>(frames.Count + (hasAudio ? 1 : 0));
        var moviDataStart = ms.Position;

        for (var i = 0; i < frames.Count; i++)
        {
            var chunkOffset = (int)(ms.Position - moviDataStart);
            WriteFourCC(ms, "00db");
            WriteInt32(ms, frameBytes);
            WriteBgrFrame(ms, frames[i], width, height, rowSize);
            PadToWord(ms);
            index.Add(new IndexEntry("00db", 0x10, chunkOffset, frameBytes));
        }

        if (hasAudio)
        {
            var audioBytes = pcmMono16!.Length * 2;
            var chunkOffset = (int)(ms.Position - moviDataStart);
            WriteFourCC(ms, "01wb");
            WriteInt32(ms, audioBytes);
            var audioSpan = new byte[audioBytes];
            for (var i = 0; i < pcmMono16.Length; i++)
                BinaryPrimitives.WriteInt16LittleEndian(audioSpan.AsSpan(i * 2), pcmMono16[i]);
            ms.Write(audioSpan);
            PadToWord(ms);
            index.Add(new IndexEntry("01wb", 0x10, chunkOffset, audioBytes));
        }

        PatchChunkSize(ms, moviSizePos);

        WriteFourCC(ms, "idx1");
        WriteInt32(ms, index.Count * 16);
        foreach (var entry in index)
        {
            WriteFourCC(ms, entry.Id);
            WriteInt32(ms, entry.Flags);
            WriteInt32(ms, entry.Offset);
            WriteInt32(ms, entry.Size);
        }

        var riffSize = (int)ms.Length - 8;
        ms.Position = 4;
        WriteInt32(ms, riffSize);
        File.WriteAllBytes(path, ms.ToArray());
    }

    static void WriteAvih(Stream s, int width, int height, int microSecPerFrame, int frameCount, int maxBytesPerSec, int streams)
    {
        WriteFourCC(s, "avih");
        WriteInt32(s, 56);
        WriteInt32(s, microSecPerFrame);
        WriteInt32(s, maxBytesPerSec);
        WriteInt32(s, 0);
        WriteInt32(s, 0x10); // AVIF_HASINDEX
        WriteInt32(s, frameCount);
        WriteInt32(s, 0);
        WriteInt32(s, streams);
        WriteInt32(s, 0);
        WriteInt32(s, width);
        WriteInt32(s, height);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
    }

    static void WriteStrhVideo(Stream s, int width, int height, int fps, int frameCount, int frameBytes)
    {
        WriteFourCC(s, "strh");
        WriteInt32(s, 56);
        WriteFourCC(s, "vids");
        WriteFourCC(s, "DIB ");
        WriteInt32(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 1);
        WriteInt32(s, fps);
        WriteInt32(s, 0);
        WriteInt32(s, frameCount);
        WriteInt32(s, frameBytes);
        WriteInt32(s, -1);
        WriteInt32(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, (short)width);
        WriteInt16(s, (short)height);
    }

    static void WriteStrfVideo(Stream s, int width, int height, int frameBytes)
    {
        WriteFourCC(s, "strf");
        WriteInt32(s, 40);
        WriteInt32(s, 40);
        WriteInt32(s, width);
        WriteInt32(s, height);
        WriteInt16(s, 1);
        WriteInt16(s, 24);
        WriteInt32(s, 0);
        WriteInt32(s, frameBytes);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 0);
    }

    static void WriteStrhAudio(Stream s, int sampleRate, int sampleCount)
    {
        WriteFourCC(s, "strh");
        WriteInt32(s, 56);
        WriteFourCC(s, "auds");
        WriteInt32(s, 1); // fccHandler WAVE_FORMAT_PCM
        WriteInt32(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
        WriteInt32(s, 0);
        WriteInt32(s, 1);
        WriteInt32(s, sampleRate);
        WriteInt32(s, 0);
        WriteInt32(s, sampleCount);
        WriteInt32(s, sampleRate * 2);
        WriteInt32(s, -1);
        WriteInt32(s, 2);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
        WriteInt16(s, 0);
    }

    static void WriteStrfAudio(Stream s, int sampleRate)
    {
        WriteFourCC(s, "strf");
        WriteInt32(s, 16);
        WriteInt16(s, 1);
        WriteInt16(s, 1);
        WriteInt32(s, sampleRate);
        WriteInt32(s, sampleRate * 2);
        WriteInt16(s, 2);
        WriteInt16(s, 16);
    }

    static void WriteBgrFrame(Stream s, VideoFrame frame, int width, int height, int rowSize)
    {
        if (frame.Width != width || frame.Height != height)
            throw new ArgumentException("Frame size mismatch.");
        if (frame.Format != VideoPixelFormat.Bgra32)
            throw new ArgumentException("Only BGRA32 frames supported.");

        var row = new byte[rowSize];
        for (var y = height - 1; y >= 0; y--)
        {
            Array.Clear(row);
            var src = y * frame.Stride;
            for (var x = 0; x < width; x++)
            {
                var si = src + x * 4;
                var di = x * 3;
                row[di] = frame.Pixels[si];
                row[di + 1] = frame.Pixels[si + 1];
                row[di + 2] = frame.Pixels[si + 2];
            }

            s.Write(row);
        }
    }

    static void PatchChunkSize(Stream s, long sizeFieldPos)
    {
        var end = s.Position;
        var size = (int)(end - sizeFieldPos - 4);
        s.Position = sizeFieldPos;
        WriteInt32(s, size);
        s.Position = end;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Defensive padding; AVI chunk sizes from this writer are always even.")]
    static void PadToWord(Stream s)
    {
        if ((s.Position & 1) != 0)
            s.WriteByte(0);
    }

    static void WriteFourCC(Stream s, string fourCC)
    {
        var bytes = Encoding.ASCII.GetBytes(fourCC);
        s.Write(bytes);
    }

    static void WriteInt32(Stream s, int value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, value);
        s.Write(b);
    }

    static void WriteInt16(Stream s, short value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(b, value);
        s.Write(b);
    }

    readonly record struct IndexEntry(string Id, int Flags, int Offset, int Size);
}
