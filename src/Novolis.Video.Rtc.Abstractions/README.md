<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-video">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
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

