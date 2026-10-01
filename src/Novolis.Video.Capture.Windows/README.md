<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-video/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-video/) · [Source](https://github.com/Novolis-Platform/novolis-video)
<!-- novolis-pkg-brand:end -->

# Novolis.Video.Capture.Windows

Windows webcam capture implementing `IVideoCaptureSource`, wrapping SIPSorceryMedia.Windows.

## Install

```bash
dotnet add package Novolis.Video.Capture.Windows
```

## Quick start

```csharp
await using var cam = new WindowsWebcamCaptureSource();
cam.FrameCaptured += frame => { /* preview */ };
await cam.StartAsync();
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `IVideoCaptureSource`, `VideoFrame` |
| `Novolis.Video.Rtc` | Mesh session that consumes capture |

