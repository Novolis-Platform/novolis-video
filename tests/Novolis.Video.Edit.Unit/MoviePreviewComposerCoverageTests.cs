using Novolis.Video.Rtc;

namespace Novolis.Video.Edit.Unit;

public sealed class MoviePreviewComposerCoverageTests
{
    [Test]
    public async Task Compose_EmptyStoryboardIsBlack()
    {
        var project = new MovieProject("Empty", 4, 2);
        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.Zero);
        await Assert.That(frame.Pixels[0]).IsEqualTo((byte)0);
        await Assert.That(frame.Pixels[1]).IsEqualTo((byte)0);
        await Assert.That(frame.Pixels[2]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Compose_WipeTransition()
    {
        var project = new MovieProject("Wipe", 8, 2);
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(0, 0, 0), TimeSpan.FromSeconds(1));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(0, 255, 0), TimeSpan.FromSeconds(1));
        var clipA = MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);
        MovieEditOps.SetOutTransition(clipA, TransitionKind.Wipe, TimeSpan.FromSeconds(0.5));

        var mid = new MoviePreviewComposer().Compose(project, TimeSpan.FromSeconds(0.75));
        await Assert.That(mid.Pixels[1]).IsEqualTo((byte)255);
    }

    [Test]
    public async Task Compose_UsesFrameProviderForImageAssets()
    {
        var project = new MovieProject("Still", 4, 2);
        var image = MovieEditOps.AddImage(project, "Img", @"C:\temp\x.bmp", TimeSpan.FromSeconds(1));
        MovieEditOps.AppendToStoryboard(project, image);

        var cache = new DecodedStillCache();
        var still = SolidColorFrames.Create(4, 2, new Rgba8(11, 22, 33));
        cache.SetStill(image.Id, still);
        await Assert.That(cache.Remove(Guid.NewGuid())).IsFalse();

        var frame = new MoviePreviewComposer(cache).Compose(project, TimeSpan.FromMilliseconds(10));
        await Assert.That(frame.Pixels[2]).IsEqualTo((byte)11);
        await Assert.That(cache.Remove(image.Id)).IsTrue();
    }

    [Test]
    public async Task Compose_AudioClipAndMissingAssetFallBack()
    {
        var project = new MovieProject("Fallback", 4, 2);
        var dir = Path.Combine(Path.GetTempPath(), "novolis-composer-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var wav = Path.Combine(dir, "t.wav");
            var audio = MovieEditOps.AddToneAudio(project, "A", wav, 440, TimeSpan.FromSeconds(0.2));
            // Place audio on video storyboard to hit MediaKind.Audio branch in RenderClip.
            MovieEditOps.AppendToStoryboard(project, audio);
            var audioFrame = new MoviePreviewComposer().Compose(project, TimeSpan.Zero);
            await Assert.That(audioFrame.Pixels[0]).IsEqualTo((byte)0);

            // Image without provider → slate color.
            var image = MovieEditOps.AddImage(project, "I", Path.Combine(dir, "missing.bmp"), TimeSpan.FromSeconds(1));
            project.MutableClips.Clear();
            MovieEditOps.AppendToStoryboard(project, image);
            var slate = new MoviePreviewComposer().Compose(project, TimeSpan.Zero);
            await Assert.That(slate.Pixels[2]).IsEqualTo((byte)32);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Compose_MissingAssetIsBlack()
    {
        var project = new MovieProject("Ghost", 2, 2);
        project.MutableClips.Add(new TimelineClip(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.Zero, TimeSpan.FromSeconds(1)));
        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.Zero);
        await Assert.That(frame.Pixels[0]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Compose_TransitionLongerThanClipIsClamped()
    {
        var project = new MovieProject("Clamp", 4, 2);
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(0, 0, 0), TimeSpan.FromSeconds(0.4));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(255, 255, 255), TimeSpan.FromSeconds(1));
        var clipA = MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);
        MovieEditOps.SetOutTransition(clipA, TransitionKind.Fade, TimeSpan.FromSeconds(2));
        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.FromSeconds(0.2));
        await Assert.That(frame.Width).IsEqualTo(4);
    }

    [Test]
    public async Task Compose_BeforeTransitionReturnsPrimary()
    {
        var project = new MovieProject("Pre", 4, 2);
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(100, 0, 0), TimeSpan.FromSeconds(2));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(0, 0, 100), TimeSpan.FromSeconds(1));
        var clipA = MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);
        MovieEditOps.SetOutTransition(clipA, TransitionKind.Fade, TimeSpan.FromSeconds(0.5));
        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.FromSeconds(0.5));
        await Assert.That(frame.Pixels[2]).IsEqualTo((byte)100);
    }

    [Test]
    public async Task Compose_RejectsNegativePosition()
    {
        var project = new MovieProject("Neg", 2, 2);
        await Assert.That(() => new MoviePreviewComposer().Compose(project, TimeSpan.FromTicks(-1)))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task DecodedStillCache_RequiresFrame()
    {
        var cache = new DecodedStillCache();
        await Assert.That(() => cache.SetStill(Guid.NewGuid(), null!)).Throws<ArgumentNullException>();
    }
}
