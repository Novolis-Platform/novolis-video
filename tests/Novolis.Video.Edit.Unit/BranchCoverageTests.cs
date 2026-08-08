using Novolis.Video.Rtc;

namespace Novolis.Video.Edit.Unit;

/// <summary>Extra branch-focused cases for the remaining coverage gaps.</summary>
public sealed class BranchCoverageTests
{
    [Test]
    public async Task Transport_WithoutChangedHandler_CoversNullEventBranches()
    {
        var transport = new EditTransport();
        transport.Play();
        transport.Pause();
        transport.Seek(TimeSpan.FromSeconds(1));
        transport.Play();
        await Assert.That(transport.Tick(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))).IsTrue();
        transport.Play();
        await Assert.That(transport.Tick(TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(5))).IsTrue();
        await Assert.That(transport.IsPlaying).IsFalse();
    }

    [Test]
    public async Task Wipe_SupportsBgr24BytesPerPixel()
    {
        var a = new VideoFrame(4, 1, 12, VideoPixelFormat.Bgr24, new byte[12]);
        var b = new VideoFrame(4, 1, 12, VideoPixelFormat.Bgr24, Enumerable.Repeat((byte)9, 12).ToArray());
        var wiped = FrameBlender.Wipe(a, b, 0.5);
        await Assert.That(wiped.Pixels[0]).IsEqualTo((byte)9);
        await Assert.That(wiped.Pixels[11]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task BmpRead_SameSizeTargetSkipsScale()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-bmp-branch-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "s.bmp");
            BmpFile.WriteBgraFrame(path, SolidColorFrames.Create(3, 2, new Rgba8(1, 2, 3)));
            var same = BmpFile.ReadToBgra(path, 3, 2);
            await Assert.That(same.Width).IsEqualTo(3);
            var onlyWidth = BmpFile.ReadToBgra(path, targetWidth: 8);
            await Assert.That(onlyWidth.Width).IsEqualTo(3);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AppendToStoryboard_UsesExplicitDuration()
    {
        var project = new MovieProject("Dur");
        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(5));
        var clip = MovieEditOps.AppendToStoryboard(project, asset, TimeSpan.FromSeconds(1.5));
        await Assert.That(clip.Duration).IsEqualTo(TimeSpan.FromSeconds(1.5));
    }

    [Test]
    public async Task TrimClip_ReturnsFalseWhenSourceOffsetExhausted()
    {
        var project = new MovieProject("Exhaust");
        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(2));
        var clip = MovieEditOps.AppendToStoryboard(project, asset);
        clip.SourceOffset = TimeSpan.FromSeconds(2);
        await Assert.That(MovieEditOps.TrimClip(project, clip.Id, TimeSpan.FromSeconds(1))).IsFalse();
    }

    [Test]
    public async Task AppendAudio_ChainsUsingAudioTrackEnd()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-audio-end-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var project = new MovieProject("Chain");
            var a = MovieEditOps.AddToneAudio(project, "A", Path.Combine(dir, "a.wav"), 220, TimeSpan.FromSeconds(0.2));
            var b = MovieEditOps.AddToneAudio(project, "B", Path.Combine(dir, "b.wav"), 330, TimeSpan.FromSeconds(0.2));
            var first = MovieEditOps.AppendAudio(project, a);
            var second = MovieEditOps.AppendAudio(project, b);
            await Assert.That(second.TimelineStart).IsEqualTo(first.TimelineEnd);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AudioMixer_SkipsNullPathAndClipsPastEnd()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-mix-branch-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var project = new MovieProject("MixBranch");
            var color = MovieEditOps.AddColorCard(project, "C", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(1));
            project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), color.Id, TimeSpan.Zero, TimeSpan.FromSeconds(1)));

            var wav = Path.Combine(dir, "late.wav");
            ToneAudio.WriteSineWav(wav, 440, TimeSpan.FromSeconds(1));
            var late = new MediaAsset(Guid.NewGuid(), "Late", MediaKind.Audio, TimeSpan.FromSeconds(1), wav);
            project.MutableAssets.Add(late);
            project.MutableAudioClips.Add(new TimelineClip(Guid.NewGuid(), late.Id, TimeSpan.FromSeconds(0.8), TimeSpan.FromSeconds(1)));

            var mixed = AudioMixer.MixMono16(project, TimeSpan.FromSeconds(1));
            await Assert.That(mixed.Length).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Composer_ClampsIncomingTimePastNextClipEnd()
    {
        var project = new MovieProject("Incoming", 4, 2);
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(0, 0, 0), TimeSpan.FromSeconds(2));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(255, 0, 0), TimeSpan.FromSeconds(0.2));
        var clipA = MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);
        MovieEditOps.SetOutTransition(clipA, TransitionKind.Fade, TimeSpan.FromSeconds(1));
        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.FromSeconds(1.9));
        await Assert.That(frame.Pixels[2]).IsGreaterThan((byte)0);
    }

    [Test]
    public async Task Contains_FalseBeforeStart()
    {
        var clip = new TimelineClip(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
        await Assert.That(clip.Contains(TimeSpan.FromSeconds(1))).IsFalse();
        var overlay = new TextOverlay(Guid.NewGuid(), "Hi", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
        await Assert.That(overlay.Contains(TimeSpan.FromSeconds(1))).IsFalse();
        var project = new MovieProject("AssetsProp");
        await Assert.That(project.Assets.Count).IsEqualTo(0);
    }
}
