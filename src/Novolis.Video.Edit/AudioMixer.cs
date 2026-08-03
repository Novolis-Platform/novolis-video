namespace Novolis.Video.Edit;

/// <summary>Mixes project audio clips into a mono PCM buffer.</summary>
public static class AudioMixer
{
    /// <summary>
    /// Returns mixed mono 16-bit samples at <see cref="ToneAudio.SampleRate"/>, or empty if no audio.
    /// </summary>
    public static short[] MixMono16(MovieProject project, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration.Ticks);

        var sampleRate = ToneAudio.SampleRate;
        var totalSamples = Math.Max(1, (int)(sampleRate * duration.TotalSeconds));
        var mix = new float[totalSamples];
        var any = false;

        foreach (var clip in project.AudioClips)
        {
            var asset = project.FindAsset(clip.AssetId);
            if (asset?.Path is null || !File.Exists(asset.Path))
                continue;

            short[] pcm;
            int rate;
            try
            {
                pcm = WavFile.ReadMono16(asset.Path, out rate);
            }
            catch
            {
                continue;
            }

            any = true;
            var start = (int)(clip.TimelineStart.TotalSeconds * sampleRate);
            for (var i = 0; i < pcm.Length; i++)
            {
                var dest = start + (rate == sampleRate
                    ? i
                    : (int)(i * (sampleRate / (double)rate)));
                if ((uint)dest >= (uint)mix.Length)
                    continue;
                mix[dest] += pcm[i] / (float)short.MaxValue;
            }
        }

        if (!any)
            return [];

        var samples = new short[totalSamples];
        for (var i = 0; i < totalSamples; i++)
            samples[i] = (short)(Math.Clamp(mix[i], -1f, 1f) * short.MaxValue);
        return samples;
    }
}
