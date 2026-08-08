using Novolis.Video.Rtc;

namespace Novolis.Video.Edit.Unit;

public sealed class AviMovieWriterAndExporterCoverageTests
{
    [Test]
    public async Task AviWriter_WritesVideoOnlyAndRejectsBadInput()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-avi-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var frame = SolidColorFrames.Create(3, 2, new Rgba8(1, 2, 3)); // odd width → padded rows
            var path = Path.Combine(dir, "v.avi");
            AviMovieWriter.Write(path, 3, 2, 10, [frame]);
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(new FileInfo(path).Length).IsGreaterThan(100);

            await Assert.That(() => AviMovieWriter.Write(path, 3, 2, 10, Array.Empty<VideoFrame>()))
                .Throws<ArgumentException>();
            await Assert.That(() => AviMovieWriter.Write(path, 3, 2, 10, [SolidColorFrames.Black(4, 2)]))
                .Throws<ArgumentException>();
            var wrongFormat = new VideoFrame(3, 2, 9, VideoPixelFormat.Bgr24, new byte[18]);
            await Assert.That(() => AviMovieWriter.Write(path, 3, 2, 10, [wrongFormat]))
                .Throws<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Exporter_RejectsEmptyStoryboard()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-export-" + Guid.NewGuid().ToString("N"));
        try
        {
            var project = new MovieProject("EmptyExport");
            await Assert.That(() => new MovieExporter().Export(project, dir)).Throws<InvalidOperationException>();
            await Assert.That(() => new MovieExporter().Export(project, " ", 12)).Throws<ArgumentException>();
            await Assert.That(() => new MovieExporter().Export(project, dir, 0)).Throws<ArgumentOutOfRangeException>();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Exporter_WritesWithoutAudioSidecar()
    {
        var dir = Path.Combine(Path.GetTempPath(), "novolis-export2-" + Guid.NewGuid().ToString("N"));
        try
        {
            var project = new MovieProject("NoAudio", 8, 4);
            MovieEditOps.AppendToStoryboard(
                project,
                MovieEditOps.AddColorCard(project, "C", new Rgba8(5, 6, 7), TimeSpan.FromSeconds(0.5)));
            var result = new MovieExporter().Export(project, dir, framesPerSecond: 4);
            await Assert.That(result.AudioPath).IsNull();
            await Assert.That(File.Exists(result.VideoPath)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "movie.json"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
