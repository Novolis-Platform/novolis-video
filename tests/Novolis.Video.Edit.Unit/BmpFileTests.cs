using Novolis.Video.Rtc;

namespace Novolis.Video.Edit.Unit;

public sealed class BmpFileTests
{
    [Test]
    public async Task WriteAndRead_RoundtripsBgra()
    {
        var dir = TempDir();
        try
        {
            var source = SolidColorFrames.Create(5, 3, new Rgba8(10, 20, 30, 255));
            var path = Path.Combine(dir, "still.bmp");
            BmpFile.WriteBgraFrame(path, source);

            var read = BmpFile.ReadToBgra(path);
            await Assert.That(read.Width).IsEqualTo(5);
            await Assert.That(read.Height).IsEqualTo(3);
            await Assert.That(read.Format).IsEqualTo(VideoPixelFormat.Bgra32);
            await Assert.That(read.Pixels[0]).IsEqualTo((byte)30);
            await Assert.That(read.Pixels[1]).IsEqualTo((byte)20);
            await Assert.That(read.Pixels[2]).IsEqualTo((byte)10);
            await Assert.That(read.Pixels[3]).IsEqualTo((byte)255);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Test]
    public async Task ReadToBgra_ScalesWhenTargetDiffers()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "g.bmp");
            BmpFile.WriteBgraFrame(path, SolidColorFrames.Create(4, 2, new Rgba8(255, 0, 0)));
            var scaled = BmpFile.ReadToBgra(path, targetWidth: 8, targetHeight: 4);
            await Assert.That(scaled.Width).IsEqualTo(8);
            await Assert.That(scaled.Height).IsEqualTo(4);
            await Assert.That(scaled.Pixels[2]).IsEqualTo((byte)255);
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Test]
    public async Task CreateGradient_InterpolatesCorners()
    {
        var frame = BmpFile.CreateGradient(4, 4, new Rgba8(0, 0, 0), new Rgba8(255, 0, 0));
        await Assert.That(frame.Width).IsEqualTo(4);
        await Assert.That(frame.Pixels[2]).IsEqualTo((byte)0);
        var last = (4 - 1) * frame.Stride + (4 - 1) * 4 + 2;
        await Assert.That(frame.Pixels[last]).IsEqualTo((byte)255);
    }

    [Test]
    public async Task CreateGradient_HandlesUnitSize()
    {
        var frame = BmpFile.CreateGradient(1, 1, new Rgba8(1, 2, 3), new Rgba8(200, 100, 50));
        await Assert.That(frame.Width).IsEqualTo(1);
        await Assert.That(frame.Height).IsEqualTo(1);
        await Assert.That(frame.Pixels[0]).IsEqualTo((byte)3);
    }

    [Test]
    public async Task ScaleNearest_Resamples()
    {
        var source = SolidColorFrames.Create(2, 2, new Rgba8(0, 255, 0));
        var scaled = BmpFile.ScaleNearest(source, 4, 1);
        await Assert.That(scaled.Width).IsEqualTo(4);
        await Assert.That(scaled.Height).IsEqualTo(1);
        await Assert.That(scaled.Pixels[1]).IsEqualTo((byte)255);
    }

    [Test]
    public async Task WriteBgraFrame_RejectsNonBgra()
    {
        var dir = TempDir();
        try
        {
            var frame = new VideoFrame(2, 1, 6, VideoPixelFormat.Bgr24, new byte[6]);
            await Assert.That(() => BmpFile.WriteBgraFrame(Path.Combine(dir, "x.bmp"), frame))
                .Throws<ArgumentException>();
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Test]
    public async Task ReadToBgra_RejectsInvalidFiles()
    {
        var dir = TempDir();
        try
        {
            var bad = Path.Combine(dir, "bad.bin");
            File.WriteAllBytes(bad, [1, 2, 3]);
            await Assert.That(() => BmpFile.ReadToBgra(bad)).Throws<InvalidDataException>();

            var notBmp = Path.Combine(dir, "not.bmp");
            File.WriteAllBytes(notBmp, new byte[60]);
            await Assert.That(() => BmpFile.ReadToBgra(notBmp)).Throws<InvalidDataException>();
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Test]
    public async Task ReadToBgra_RejectsNon24Bit()
    {
        var dir = TempDir();
        try
        {
            var path = Path.Combine(dir, "32.bmp");
            // Minimal BMP header claiming 32 bpp.
            var bytes = new byte[54];
            bytes[0] = (byte)'B';
            bytes[1] = (byte)'M';
            bytes[18] = 1;
            bytes[22] = 1;
            bytes[28] = 32;
            File.WriteAllBytes(path, bytes);
            await Assert.That(() => BmpFile.ReadToBgra(path)).Throws<InvalidDataException>();
        }
        finally
        {
            Cleanup(dir);
        }
    }

    [Test]
    public async Task WriteBgraFrame_RejectsNullOrEmptyPath()
    {
        var frame = SolidColorFrames.Black(1, 1);
        await Assert.That(() => BmpFile.WriteBgraFrame(" ", frame)).Throws<ArgumentException>();
        await Assert.That(() => BmpFile.WriteBgraFrame(Path.Combine(Path.GetTempPath(), "x.bmp"), null!))
            .Throws<ArgumentNullException>();
        await Assert.That(() => BmpFile.ScaleNearest(null!, 1, 1)).Throws<ArgumentNullException>();
    }

    static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-bmp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }
}
