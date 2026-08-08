namespace Novolis.Video.Edit.Unit;

public sealed class MovieEditOpsCoverageTests
{
    [Test]
    public async Task AddImageVideoAndRemoveClip_Work()
    {
        var project = new MovieProject("Ops");
        var image = MovieEditOps.AddImage(project, "Img", @"C:\temp\img.bmp", TimeSpan.FromSeconds(3));
        var video = MovieEditOps.AddVideo(project, "Vid", @"C:\temp\vid.avi", TimeSpan.FromSeconds(5));
        await Assert.That(image.Kind).IsEqualTo(MediaKind.Image);
        await Assert.That(video.Kind).IsEqualTo(MediaKind.Video);

        var clip = MovieEditOps.AppendToStoryboard(project, image);
        MovieEditOps.AppendToStoryboard(project, video);
        await Assert.That(MovieEditOps.RemoveClip(project, clip.Id)).IsTrue();
        await Assert.That(project.Clips.Count).IsEqualTo(1);
        await Assert.That(project.Clips[0].TimelineStart).IsEqualTo(TimeSpan.Zero);
        await Assert.That(MovieEditOps.RemoveClip(project, Guid.NewGuid())).IsFalse();
    }

    [Test]
    public async Task RemoveClip_WithoutCompactLeavesGap()
    {
        var project = new MovieProject("Gap");
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 2, 3), TimeSpan.FromSeconds(2));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(4, 5, 6), TimeSpan.FromSeconds(2));
        var first = MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);
        await Assert.That(MovieEditOps.RemoveClip(project, first.Id, compact: false)).IsTrue();
        await Assert.That(project.Clips[0].TimelineStart).IsEqualTo(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task SplitAt_ReturnsNullAtBoundaries()
    {
        var project = new MovieProject("Split");
        var card = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(4));
        MovieEditOps.AppendToStoryboard(project, card);
        await Assert.That(MovieEditOps.SplitAt(project, TimeSpan.FromSeconds(10))).IsNull();
        await Assert.That(MovieEditOps.SplitAt(project, TimeSpan.Zero)).IsNull();
        await Assert.That(MovieEditOps.SplitAt(project, TimeSpan.FromSeconds(4))).IsNull();
    }

    [Test]
    public async Task TrimClip_ClampsAndCompacts()
    {
        var project = new MovieProject("Trim");
        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(9, 9, 9), TimeSpan.FromSeconds(5));
        var clip = MovieEditOps.AppendToStoryboard(project, asset);
        MovieEditOps.AppendToStoryboard(project, MovieEditOps.AddColorCard(project, "B", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(2)));

        await Assert.That(MovieEditOps.TrimClip(project, clip.Id, TimeSpan.FromSeconds(2))).IsTrue();
        await Assert.That(clip.Duration).IsEqualTo(TimeSpan.FromSeconds(2));
        await Assert.That(project.Clips[1].TimelineStart).IsEqualTo(TimeSpan.FromSeconds(2));

        await Assert.That(MovieEditOps.TrimClip(project, Guid.NewGuid(), TimeSpan.FromSeconds(1))).IsFalse();
        await Assert.That(MovieEditOps.TrimClip(project, clip.Id, TimeSpan.FromSeconds(100))).IsTrue();
        await Assert.That(clip.Duration).IsEqualTo(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task TrimClip_WithoutCompactAndRejectsZero()
    {
        var project = new MovieProject("Trim2");
        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(3));
        var clip = MovieEditOps.AppendToStoryboard(project, asset);
        MovieEditOps.AppendToStoryboard(project, MovieEditOps.AddColorCard(project, "B", new Rgba8(2, 2, 2), TimeSpan.FromSeconds(2)));
        await Assert.That(MovieEditOps.TrimClip(project, clip.Id, TimeSpan.FromSeconds(1), compact: false)).IsTrue();
        await Assert.That(project.Clips[1].TimelineStart).IsEqualTo(TimeSpan.FromSeconds(3));
        await Assert.That(() => MovieEditOps.TrimClip(project, clip.Id, TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task AppendAudio_RejectsNonAudioAndForeignAsset()
    {
        var project = new MovieProject("Audio");
        var color = MovieEditOps.AddColorCard(project, "C", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(1));
        await Assert.That(() => MovieEditOps.AppendAudio(project, color)).Throws<ArgumentException>();

        var foreign = new MediaAsset(Guid.NewGuid(), "Tone", MediaKind.Audio, TimeSpan.FromSeconds(1), @"C:\x.wav");
        await Assert.That(() => MovieEditOps.AppendAudio(project, foreign)).Throws<InvalidOperationException>();
        await Assert.That(() => MovieEditOps.AppendToStoryboard(project, foreign)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AppendAudio_UsesExplicitStart()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-audio-ops-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var project = new MovieProject("AudioStart");
            var wav = Path.Combine(dir, "t.wav");
            var audio = MovieEditOps.AddToneAudio(project, "T", wav, 220, TimeSpan.FromSeconds(0.2));
            var clip = MovieEditOps.AppendAudio(project, audio, start: TimeSpan.FromSeconds(1));
            await Assert.That(clip.TimelineStart).IsEqualTo(TimeSpan.FromSeconds(1));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AddTextOverlay_UsesCustomColorAndAnchors()
    {
        var project = new MovieProject("Text");
        var overlay = MovieEditOps.AddTextOverlay(
            project,
            "Title",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            new Rgba8(1, 2, 3),
            anchorX: 0.1,
            anchorY: 0.2);
        await Assert.That(overlay.Color).IsEqualTo(new Rgba8(1, 2, 3));
        await Assert.That(overlay.AnchorX).IsEqualTo(0.1);
        await Assert.That(overlay.Contains(TimeSpan.FromMilliseconds(500))).IsTrue();
        await Assert.That(overlay.Contains(TimeSpan.FromSeconds(2))).IsFalse();
    }

    [Test]
    public async Task SetOutTransition_RejectsNegativeDuration()
    {
        var project = new MovieProject("X");
        var clip = MovieEditOps.AppendToStoryboard(
            project,
            MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(1)));
        await Assert.That(() => MovieEditOps.SetOutTransition(clip, TransitionKind.Fade, TimeSpan.FromTicks(-1)))
            .Throws<ArgumentOutOfRangeException>();
    }
}
