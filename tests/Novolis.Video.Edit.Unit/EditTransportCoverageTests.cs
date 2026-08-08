namespace Novolis.Video.Edit.Unit;

public sealed class EditTransportCoverageTests
{
    [Test]
    public async Task PlayPauseToggleSeek_DriveChangedAndIdempotentPaths()
    {
        var transport = new EditTransport();
        var changes = 0;
        transport.Changed += () => changes++;

        transport.Play();
        await Assert.That(transport.IsPlaying).IsTrue();
        transport.Play(); // already playing
        await Assert.That(changes).IsEqualTo(1);

        transport.Pause();
        await Assert.That(transport.IsPlaying).IsFalse();
        transport.Pause(); // already paused
        await Assert.That(changes).IsEqualTo(2);

        transport.Toggle();
        await Assert.That(transport.IsPlaying).IsTrue();
        transport.Toggle();
        await Assert.That(transport.IsPlaying).IsFalse();
        await Assert.That(changes).IsEqualTo(4);

        transport.Seek(TimeSpan.FromSeconds(1.5));
        await Assert.That(transport.Position).IsEqualTo(TimeSpan.FromSeconds(1.5));
        transport.Seek(TimeSpan.FromSeconds(1.5)); // same position
        await Assert.That(changes).IsEqualTo(5);
    }

    [Test]
    public async Task Tick_ReturnsFalseWhenPausedOrZeroDelta()
    {
        var transport = new EditTransport();
        await Assert.That(transport.Tick(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10))).IsFalse();

        transport.Play();
        await Assert.That(transport.Tick(TimeSpan.Zero, TimeSpan.FromSeconds(10))).IsFalse();
        await Assert.That(transport.IsPlaying).IsTrue();
    }

    [Test]
    public async Task Seek_RejectsNegative()
    {
        var transport = new EditTransport();
        await Assert.That(() => transport.Seek(TimeSpan.FromTicks(-1))).Throws<ArgumentOutOfRangeException>();
    }
}
