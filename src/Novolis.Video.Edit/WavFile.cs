using System.Buffers.Binary;

namespace Novolis.Video.Edit;

/// <summary>Minimal mono PCM WAV writer/reader for demo export.</summary>
public static class WavFile
{
    public static void WriteMono16(string path, int sampleRate, ReadOnlySpan<short> samples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        var dataBytes = samples.Length * 2;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var fs = File.Create(path);
        Span<byte> header = stackalloc byte[44];
        WriteAscii(header, 0, "RIFF");
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], 36 + dataBytes);
        WriteAscii(header, 8, "WAVE");
        WriteAscii(header, 12, "fmt ");
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(header[20..], 1); // PCM
        BinaryPrimitives.WriteInt16LittleEndian(header[22..], 1); // mono
        BinaryPrimitives.WriteInt32LittleEndian(header[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(header[28..], sampleRate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(header[32..], 2);
        BinaryPrimitives.WriteInt16LittleEndian(header[34..], 16);
        WriteAscii(header, 36, "data");
        BinaryPrimitives.WriteInt32LittleEndian(header[40..], dataBytes);
        fs.Write(header);

        Span<byte> sampleBytes = stackalloc byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
            BinaryPrimitives.WriteInt16LittleEndian(sampleBytes[(i * 2)..], samples[i]);
        fs.Write(sampleBytes);
    }

    public static short[] ReadMono16(string path, out int sampleRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 44)
            throw new InvalidDataException("WAV too small.");

        sampleRate = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24));
        var dataSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(40));
        var samples = new short[dataSize / 2];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(44 + i * 2));
        return samples;
    }

    static void WriteAscii(Span<byte> dest, int offset, string text)
    {
        for (var i = 0; i < text.Length; i++)
            dest[offset + i] = (byte)text[i];
    }
}
