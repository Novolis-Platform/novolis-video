<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-video">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
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

