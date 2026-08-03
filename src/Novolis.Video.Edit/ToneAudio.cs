namespace Novolis.Video.Edit;

/// <summary>Generates simple mono PCM tones for demo audio tracks.</summary>
public static class ToneAudio
{
    public const int SampleRate = 22_050;

    /// <summary>Writes a mono 16-bit WAV sine tone to <paramref name="path"/>.</summary>
    public static void WriteSineWav(string path, double frequencyHz, TimeSpan duration, double amplitude = 0.25)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frequencyHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);
        amplitude = Math.Clamp(amplitude, 0, 1);

        var samples = (int)(SampleRate * duration.TotalSeconds);
        var pcm = new short[samples];
        for (var i = 0; i < samples; i++)
        {
            var t = i / (double)SampleRate;
            pcm[i] = (short)(Math.Sin(2 * Math.PI * frequencyHz * t) * amplitude * short.MaxValue);
        }

        WavFile.WriteMono16(path, SampleRate, pcm);
    }
}
