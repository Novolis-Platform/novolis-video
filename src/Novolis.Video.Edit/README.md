<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-video">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Video.Edit

Avalonia-free editing core for a minimal Windows Movie Maker–style app.

## Features

- Collections + storyboard clips
- Audio track + tone WAV helper
- Fade / wipe transitions
- Text overlays (bitmap-font burn-in by default)
- Export: playable `movie.avi` (BGR24 + PCM) + optional `audio.wav` + JSON manifest

## Quick start

```csharp
var project = new MovieProject("Demo");
var a = MovieEditOps.AddColorCard(project, "A", new Rgba8(20, 80, 120), TimeSpan.FromSeconds(2));
var b = MovieEditOps.AddColorCard(project, "B", new Rgba8(120, 40, 40), TimeSpan.FromSeconds(2));
var clipA = MovieEditOps.AppendToStoryboard(project, a);
MovieEditOps.AppendToStoryboard(project, b);
MovieEditOps.SetOutTransition(clipA, TransitionKind.Fade, TimeSpan.FromSeconds(0.5));
MovieEditOps.AddTextOverlay(project, "Hello", TimeSpan.Zero, TimeSpan.FromSeconds(1.5));

var tone = MovieEditOps.AddToneAudio(project, "Bed", "bed.wav", 220, TimeSpan.FromSeconds(4));
MovieEditOps.AppendAudio(project, tone);

var result = new MovieExporter().Export(project, @"D:\out\movie");
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Avalonia.Video` | Workspace, storyboard, preview session |

## Install

```bash
dotnet add package Novolis.Video.Edit
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).


