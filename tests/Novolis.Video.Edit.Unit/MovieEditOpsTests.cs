namespace Novolis.Video.Edit.Unit;

public sealed class MovieEditOpsTests
{
    [Test]
    public async Task AppendAndSplit_CompactsStoryboard()
    {
        var project = new MovieProject("Unit");
        var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(255, 0, 0), TimeSpan.FromSeconds(4));
        var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(0, 255, 0), TimeSpan.FromSeconds(2));
        MovieEditOps.AppendToStoryboard(project, a);
        MovieEditOps.AppendToStoryboard(project, b);

        await Assert.That(StoryboardQuery.TotalDuration(project)).IsEqualTo(TimeSpan.FromSeconds(6));

        var right = MovieEditOps.SplitAt(project, TimeSpan.FromSeconds(2));
        await Assert.That(right).IsNotNull();
        await Assert.That(project.Clips.Count).IsEqualTo(3);
        await Assert.That(project.Clips[0].Duration).IsEqualTo(TimeSpan.FromSeconds(2));
        await Assert.That(project.Clips[1].TimelineStart).IsEqualTo(TimeSpan.FromSeconds(2));
        await Assert.That(StoryboardQuery.TotalDuration(project)).IsEqualTo(TimeSpan.FromSeconds(6));
    }

    [Test]
    public async Task Transport_TicksAndStopsAtEnd()
    {
        var transport = new EditTransport();
        transport.Play();
        transport.Tick(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2.5));
        await Assert.That(transport.Position).IsEqualTo(TimeSpan.FromSeconds(1));
        await Assert.That(transport.IsPlaying).IsTrue();

        transport.Tick(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2.5));
        await Assert.That(transport.Position).IsEqualTo(TimeSpan.FromSeconds(2.5));
        await Assert.That(transport.IsPlaying).IsFalse();
    }

    [Test]
    public async Task Composer_EmitsColorFrames()
    {
        var project = new MovieProject("Unit", 8, 4);
        var card = MovieEditOps.AddColorCard(project, "Blue", new Rgba8(10, 20, 30), TimeSpan.FromSeconds(1));
        MovieEditOps.AppendToStoryboard(project, card);

        var frame = new MoviePreviewComposer().Compose(project, TimeSpan.FromMilliseconds(100));
        await Assert.That(frame.Width).IsEqualTo(8);
        await Assert.That(frame.Height).IsEqualTo(4);
        await Assert.That(frame.Pixels[0]).IsEqualTo((byte)30); // B
        await Assert.That(frame.Pixels[1]).IsEqualTo((byte)20); // G
        await Assert.That(frame.Pixels[2]).IsEqualTo((byte)10); // R
    }
}
