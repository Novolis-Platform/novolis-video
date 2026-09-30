using System.Text.Json;
using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Result of <see cref="MovieExporter.Export"/>.</summary>
public sealed record MovieExportResult(
    string OutputDirectory,
    string VideoPath,
    int FrameCount,
    double FramesPerSecond,
    string? AudioPath);
