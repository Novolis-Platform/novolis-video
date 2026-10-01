<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-video/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-video/) · [Source](https://github.com/Novolis-Platform/novolis-video)
<!-- novolis-pkg-brand:end -->

# Novolis.Video.Rtc.Abstractions

Platform-neutral contracts for mesh realtime video: frames, signaling DTOs, and `IRtcMeshSession`.

## Install

```bash
dotnet add package Novolis.Video.Rtc.Abstractions
```

## Quick start

Implement or consume `IRtcMeshSession`. The host application supplies signaling (e.g. SignalR); this package does not open sockets.

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc` | SIPSorcery-backed mesh session |
| `Novolis.Video.Capture.Windows` | Webcam capture for Windows |
| `Novolis.Avalonia.Video` | Avalonia `VideoSurface` control |

