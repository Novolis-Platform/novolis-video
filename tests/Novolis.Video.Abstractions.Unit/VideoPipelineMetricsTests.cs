namespace Novolis.Video.Abstractions.Unit;

public sealed class VideoPipelineMetricsTests
{
    [Test]
    public async Task MetricsKeepBoundedFrameAndRoundTripSamples()
    {
        var metrics = new VideoPipelineMetrics();
        var sourceTicks = DateTime.UtcNow.Ticks
            - TimeSpan.FromMilliseconds(25).Ticks;

        metrics.RecordCaptured();
        metrics.RecordEncoded(4.5);
        metrics.RecordReceived(128, sourceTicks);
        metrics.RecordDecoded(3.2);
        metrics.RecordPresented(1.1, sourceTicks);
        metrics.RecordInputRoundTrip(42);
        metrics.RecordKeyFrameRequest();

        var snapshot = metrics.Snapshot();

        await Assert.That(snapshot.CapturedFrames).IsEqualTo(1);
        await Assert.That(snapshot.EncodedFrames).IsEqualTo(1);
        await Assert.That(snapshot.ReceivedFrames).IsEqualTo(1);
        await Assert.That(snapshot.PresentedFrames).IsEqualTo(1);
        await Assert.That(snapshot.KeyFrameRequests).IsEqualTo(1);
        await Assert.That(snapshot.FrameAgeP95Milliseconds.GetValueOrDefault())
            .IsGreaterThan(0);
        await Assert.That(snapshot.InputRoundTripP95Milliseconds).IsEqualTo(42);
    }

    [Test]
    public async Task SampleWindowReportsPercentile()
    {
        var window = new LatencySampleWindow(8);
        window.Add(10);
        window.Add(20);
        window.Add(30);

        await Assert.That(window.Percentile(0.50)).IsEqualTo(20);
        await Assert.That(window.Percentile(0.95)).IsEqualTo(30);
    }
}
