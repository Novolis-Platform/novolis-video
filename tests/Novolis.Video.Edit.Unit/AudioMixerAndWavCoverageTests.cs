namespace Novolis.Video.Edit.Unit;

public sealed class AudioMixerAndWavCoverageTests
{
    [Test]
    public async Task MixMono16_ReturnsEmptyWithoutUsableAudio()
    {
        var project = new MovieProject("Silent");
        var missing = new MediaAsset(Guid.NewGuid(), "M", MediaKind.Audio, TimeSpan.FromSeconds(1), @"C:\missing\nope.wav");
        project.MutableAssets.Add(missing);
        project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), missing.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1)));

        var empty = AudioMixer.MixMono16(project, TimeSpan.FromSeconds(0.5));
        await Assert.That(empty.Length).IsEqualTo(0);
    }

    [Test]
    public async Task MixMono16_SkipsCorruptWavAndMixesValid()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-mix-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var bad = Path.Combine(dir, "bad.wav");
            File.WriteAllBytes(bad, [1, 2, 3]);
            var good = Path.Combine(dir, "good.wav");
            ToneAudio.WriteSineWav(good, 440, TimeSpan.FromSeconds(0.2));

            var project = new MovieProject("Mix");
            var corrupt = new MediaAsset(Guid.NewGuid(), "Corrupt", MediaKind.Audio, TimeSpan.FromSeconds(0.1), bad);
            var ok = new MediaAsset(Guid.NewGuid(), "Ok", MediaKind.Audio, TimeSpan.FromSeconds(0.2), good);
            project.MutableAssets.Add(corrupt);
            project.MutableAssets.Add(ok);
            project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), corrupt.Id, TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
            project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), ok.Id, TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.2)));

            var mixed = AudioMixer.MixMono16(project, TimeSpan.FromSeconds(0.4));
            await Assert.That(mixed.Length).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task MixMono16_ResamplesDifferentRate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-resample-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "slow.wav");
            // Write at half the mixer rate so resampling branch runs.
            var samples = new short[11025];
            for (var i = 0; i < samples.Length; i++)
                samples[i] = (short)(i % 100);
            WavFile.WriteMono16(path, ToneAudio.SampleRate / 2, samples);

            var project = new MovieProject("Rate");
            var asset = new MediaAsset(Guid.NewGuid(), "S", MediaKind.Audio, TimeSpan.FromSeconds(1), path);
            project.MutableAssets.Add(asset);
            project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), asset.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1)));

            var mixed = AudioMixer.MixMono16(project, TimeSpan.FromSeconds(1));
            await Assert.That(mixed.Length).IsEqualTo(ToneAudio.SampleRate);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task WavFile_ReadRejectsTooSmall()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-wav-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "tiny.wav");
            File.WriteAllBytes(path, new byte[10]);
            await Assert.That(() => WavFile.ReadMono16(path, out _)).Throws<InvalidDataException>();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task WavFile_RoundtripsEmptySamples()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-wav2-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "empty.wav");
            WavFile.WriteMono16(path, 8000, ReadOnlySpan<short>.Empty);
            var pcm = WavFile.ReadMono16(path, out var rate);
            await Assert.That(rate).IsEqualTo(8000);
            await Assert.That(pcm.Length).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
