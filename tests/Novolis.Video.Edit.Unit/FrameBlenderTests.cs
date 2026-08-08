using Novolis.Video.Rtc;

namespace Novolis.Video.Edit.Unit;

public sealed class FrameBlenderTests
{
    [Test]
    public async Task Fade_BlendsAndClampsAmount()
    {
        var a = SolidColorFrames.Create(2, 1, new Rgba8(0, 0, 0));
        var b = SolidColorFrames.Create(2, 1, new Rgba8(255, 0, 0));
        var mid = FrameBlender.Fade(a, b, 0.5);
        await Assert.That(mid.Pixels[2]).IsGreaterThan((byte)100);
        await Assert.That(mid.Pixels[2]).IsLessThan((byte)160);

        var onlyB = FrameBlender.Fade(a, b, 2.0);
        await Assert.That(onlyB.Pixels[2]).IsEqualTo((byte)255);
        var onlyA = FrameBlender.Fade(a, b, -1.0);
        await Assert.That(onlyA.Pixels[2]).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Wipe_CopiesLeftFromB()
    {
        var a = SolidColorFrames.Create(4, 2, new Rgba8(0, 0, 0));
        var b = SolidColorFrames.Create(4, 2, new Rgba8(0, 255, 0));
        var wiped = FrameBlender.Wipe(a, b, 0.5);
        await Assert.That(wiped.Pixels[1]).IsEqualTo((byte)255); // left from B (G)
        var right = wiped.Stride - 4 + 1;
        await Assert.That(wiped.Pixels[right]).IsEqualTo((byte)0); // right from A
    }

    [Test]
    public async Task FadeAndWipe_RejectMismatchedFrames()
    {
        var a = SolidColorFrames.Black(2, 2);
        var b = SolidColorFrames.Black(3, 2);
        await Assert.That(() => FrameBlender.Fade(a, b, 0.5)).Throws<ArgumentException>();
        await Assert.That(() => FrameBlender.Wipe(a, b, 0.5)).Throws<ArgumentException>();
        await Assert.That(() => FrameBlender.Fade(null!, a, 0.5)).Throws<ArgumentNullException>();
        await Assert.That(() => FrameBlender.Wipe(a, null!, 0.5)).Throws<ArgumentNullException>();
    }
}
