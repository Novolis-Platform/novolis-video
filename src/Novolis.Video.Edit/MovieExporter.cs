using System.Text.Json;
using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Exports a project as a playable AVI (plus optional sidecars).</summary>
public sealed class MovieExporter
{
    readonly MoviePreviewComposer _composer;

    public MovieExporter(MoviePreviewComposer? composer = null)
    {
        _composer = composer ?? new MoviePreviewComposer();
    }

    /// <summary>
    /// Writes <c>movie.avi</c> (video + audio), <c>movie.json</c>, and optional <c>audio.wav</c>
    /// under <paramref name="outputDirectory"/>.
    /// </summary>
    public MovieExportResult Export(
        MovieProject project,
        string outputDirectory,
        double framesPerSecond = 12)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);

        var duration = StoryboardQuery.TotalDuration(project);
        if (duration <= TimeSpan.Zero)
            throw new InvalidOperationException("Project has no storyboard duration.");

        Directory.CreateDirectory(outputDirectory);

        var frameCount = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds * framesPerSecond));
        var frames = new List<VideoFrame>(frameCount);
        for (var i = 0; i < frameCount; i++)
        {
            var t = TimeSpan.FromSeconds(Math.Min(duration.TotalSeconds - 1e-6, i / framesPerSecond));
            frames.Add(_composer.Compose(project, t));
        }

        var pcm = AudioMixer.MixMono16(project, duration);
        var aviPath = Path.Combine(outputDirectory, "movie.avi");
        AviMovieWriter.Write(
            aviPath,
            project.Width,
            project.Height,
            framesPerSecond,
            frames,
            pcm.Length > 0 ? pcm : null);

        string? audioPath = null;
        if (pcm.Length > 0)
        {
            audioPath = Path.Combine(outputDirectory, "audio.wav");
            WavFile.WriteMono16(audioPath, ToneAudio.SampleRate, pcm);
        }

        var manifestPath = Path.Combine(outputDirectory, "movie.json");
        var manifest = new
        {
            project.Title,
            project.Width,
            project.Height,
            FramesPerSecond = framesPerSecond,
            FrameCount = frameCount,
            DurationSeconds = duration.TotalSeconds,
            VideoFile = "movie.avi",
            AudioFile = audioPath is null ? null : "audio.wav",
            TextOverlays = project.TextOverlays.Select(o => new
            {
                o.Text,
                Start = o.TimelineStart.TotalSeconds,
                Duration = o.Duration.TotalSeconds,
            }).ToArray(),
            Transitions = project.Clips.Select(c => new
            {
                c.Id,
                Out = c.OutTransition.ToString(),
                Seconds = c.OutTransitionDuration.TotalSeconds,
            }).ToArray(),
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

        return new MovieExportResult(outputDirectory, aviPath, frameCount, framesPerSecond, audioPath);
    }
}

/// <summary>Result of <see cref="MovieExporter.Export"/>.</summary>
public sealed record MovieExportResult(
    string OutputDirectory,
    string VideoPath,
    int FrameCount,
    double FramesPerSecond,
    string? AudioPath);
