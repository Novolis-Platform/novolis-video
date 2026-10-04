namespace Novolis.Video.Abstractions.Unit;

public sealed class VideoAdaptiveProfileControllerTests
{
    [Test]
    public async Task ControllerDowngradesUnderPressureAndUpgradesAfterStability()
    {
        var controller = new VideoAdaptiveProfileController(
            1920,
            1080,
            VideoAdaptiveProfileKind.High);

        var downgraded = controller.Observe(
            droppedFrames: 1,
            receivedFrames: 10,
            frameAgeP95Milliseconds: 250,
            inputRoundTripP95Milliseconds: 220,
            now: DateTimeOffset.UtcNow.AddSeconds(5));

        await Assert.That(downgraded).IsNotNull();
        await Assert.That(downgraded!.Kind).IsEqualTo(VideoAdaptiveProfileKind.Medium);
        await Assert.That(downgraded.Width).IsLessThanOrEqualTo(720);

        var upgraded = controller.Observe(
            droppedFrames: 1,
            receivedFrames: 10,
            frameAgeP95Milliseconds: 20,
            inputRoundTripP95Milliseconds: 20,
            now: DateTimeOffset.UtcNow.AddSeconds(20));

        await Assert.That(upgraded).IsNotNull();
        await Assert.That(upgraded!.Kind).IsEqualTo(VideoAdaptiveProfileKind.High);
    }
}
