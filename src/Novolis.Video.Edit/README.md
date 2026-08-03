# Novolis.Video.Edit

Avalonia-free editing core for a minimal Windows Movie Maker–style app: media collections, storyboard clips, playhead transport, and solid-color preview frames.

## Install

```bash
dotnet add package Novolis.Video.Edit
```

## Quick start

```csharp
var project = new MovieProject("Demo");
var card = MovieEditOps.AddColorCard(project, "Title", new Rgba8(20, 80, 120), TimeSpan.FromSeconds(3));
MovieEditOps.AppendToStoryboard(project, card);

var transport = new EditTransport();
transport.Play();
transport.Tick(TimeSpan.FromMilliseconds(33), StoryboardQuery.TotalDuration(project));

var composer = new MoviePreviewComposer();
var frame = composer.Compose(project, transport.Position); // VideoFrame
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Avalonia.Video` | `VideoSurface`, storyboard strip, preview session |
