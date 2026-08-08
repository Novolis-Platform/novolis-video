namespace Novolis.Video.Edit.Unit;

public sealed class MediaAssetProjectAndQueryCoverageTests
{
    [Test]
    public async Task MediaAsset_ValidatesColorAndPathKinds()
    {
        await Assert.That(() => new MediaAsset(Guid.NewGuid(), "c", MediaKind.Color, TimeSpan.FromSeconds(1)))
            .Throws<ArgumentException>();
        await Assert.That(() => new MediaAsset(Guid.NewGuid(), "i", MediaKind.Image, TimeSpan.FromSeconds(1)))
            .Throws<ArgumentException>();
        await Assert.That(() => new MediaAsset(Guid.NewGuid(), " ", MediaKind.Video, TimeSpan.FromSeconds(1), "x"))
            .Throws<ArgumentException>();
        await Assert.That(() => new MediaAsset(Guid.NewGuid(), "a", MediaKind.Audio, TimeSpan.Zero, "x.wav"))
            .Throws<ArgumentOutOfRangeException>();

        var ok = new MediaAsset(Guid.NewGuid(), "ok", MediaKind.Color, TimeSpan.FromSeconds(1), color: new Rgba8(1, 2, 3));
        ok.Name = "renamed";
        ok.Duration = TimeSpan.FromSeconds(2);
        await Assert.That(ok.Name).IsEqualTo("renamed");
        await Assert.That(ok.Color).IsEqualTo(new Rgba8(1, 2, 3));
    }

    [Test]
    public async Task MovieProject_FindMissesAndRejectsBadCtor()
    {
        await Assert.That(() => new MovieProject(" ", 10, 10)).Throws<ArgumentException>();
        await Assert.That(() => new MovieProject("T", 0, 10)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new MovieProject("T", 10, 0)).Throws<ArgumentOutOfRangeException>();

        var project = new MovieProject("Find", 16, 9);
        project.Title = "Updated";
        project.Width = 32;
        project.Height = 18;
        await Assert.That(project.FindAsset(Guid.NewGuid())).IsNull();
        await Assert.That(project.FindClip(Guid.NewGuid())).IsNull();

        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(1, 1, 1), TimeSpan.FromSeconds(1));
        var clip = MovieEditOps.AppendToStoryboard(project, asset);
        await Assert.That(project.FindAsset(asset.Id)).IsEqualTo(asset);
        await Assert.That(project.FindClip(clip.Id)).IsEqualTo(clip);
    }

    [Test]
    public async Task StoryboardQuery_AssetAtAndMisses()
    {
        var project = new MovieProject("Q");
        await Assert.That(StoryboardQuery.ClipAt(project, TimeSpan.Zero)).IsNull();
        await Assert.That(StoryboardQuery.AssetAt(project, TimeSpan.Zero)).IsNull();
        await Assert.That(StoryboardQuery.TotalDuration(project)).IsEqualTo(TimeSpan.Zero);

        var asset = MovieEditOps.AddColorCard(project, "A", new Rgba8(9, 8, 7), TimeSpan.FromSeconds(2));
        MovieEditOps.AppendToStoryboard(project, asset);
        await Assert.That(StoryboardQuery.AssetAt(project, TimeSpan.FromSeconds(1))).IsEqualTo(asset);
        await Assert.That(StoryboardQuery.AssetAt(project, TimeSpan.FromSeconds(5))).IsNull();
    }

    [Test]
    public async Task TimelineClip_SourceTimeAndContainsEdges()
    {
        var clip = new TimelineClip(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0.5));
        await Assert.That(clip.Contains(TimeSpan.FromSeconds(1))).IsTrue();
        await Assert.That(clip.Contains(TimeSpan.FromSeconds(3))).IsFalse();
        await Assert.That(clip.SourceTimeAt(TimeSpan.FromSeconds(2))).IsEqualTo(TimeSpan.FromSeconds(1.5));
        await Assert.That(() => new TimelineClip(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromTicks(-1), TimeSpan.FromSeconds(1)))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TextOverlay_RejectsBadAnchors()
    {
        await Assert.That(() => new TextOverlay(Guid.NewGuid(), "x", TimeSpan.Zero, TimeSpan.FromSeconds(1), anchorX: -0.1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new TextOverlay(Guid.NewGuid(), "x", TimeSpan.Zero, TimeSpan.FromSeconds(1), anchorY: 1.1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new TextOverlay(Guid.NewGuid(), " ", TimeSpan.Zero, TimeSpan.FromSeconds(1)))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task BitmapFontOverlay_DrawsKnownAndUnknownGlyphs()
    {
        var frame = SolidColorFrames.Black(80, 40);
        var overlay = new TextOverlay(Guid.NewGuid(), "A?!", TimeSpan.Zero, TimeSpan.FromSeconds(1), new Rgba8(255, 0, 0), 0.5, 0.5);
        new BitmapFontOverlay().Apply(frame, [overlay]);
        await Assert.That(frame.Pixels.Any(b => b == 255)).IsTrue();
        await Assert.That(() => new BitmapFontOverlay().Apply(null!, [overlay])).Throws<ArgumentNullException>();
        await Assert.That(() => new BitmapFontOverlay().Apply(frame, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task SolidColorFrames_RejectsBadSize()
    {
        await Assert.That(() => SolidColorFrames.Create(0, 1, new Rgba8(1, 1, 1))).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => SolidColorFrames.Black(1, 0)).Throws<ArgumentOutOfRangeException>();
    }
}
