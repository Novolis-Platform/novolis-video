namespace Novolis.Video.Abstractions.Unit;

public sealed class VideoStreamGateTests
{
    [Test]
    public async Task ResetRejectsDeltaFramesUntilAnIntraFrameArrives()
    {
        var gate = new VideoStreamGate();
        gate.RequireKeyFrame();

        await Assert.That(gate.TryAccept(isKeyFrame: false, out _)).IsFalse();
        await Assert.That(gate.TryAccept(
                CreateFrame(isKeyFrame: true),
                out var generation))
            .IsTrue();
        await Assert.That(gate.IsCurrent(generation)).IsTrue();
        await Assert.That(gate.TryAccept(isKeyFrame: false, out _)).IsTrue();
    }

    [Test]
    public async Task NewResetMakesDecodedWorkFromThePreviousStreamStale()
    {
        var gate = new VideoStreamGate();

        await Assert.That(gate.TryAccept(
                CreateFrame(isKeyFrame: true),
                out var previousGeneration))
            .IsTrue();
        gate.RequireKeyFrame();

        await Assert.That(gate.IsCurrent(previousGeneration)).IsFalse();
        await Assert.That(gate.TryAccept(isKeyFrame: false, out _)).IsFalse();
    }

    private static EncodedVideoFrame CreateFrame(bool isKeyFrame) =>
        new(16, 16, 1, [1, 2, 3], isKeyFrame);
}
