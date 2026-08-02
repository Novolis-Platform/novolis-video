# novolis-video

Realtime video for the Novolis platform: mesh RTC contracts, SIPSorcery-backed mesh sessions, and Windows capture.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Media.Rtc.Abstractions` | `VideoFrame`, `IRtcMeshSession`, signal DTOs |
| `Novolis.Media.Rtc` | SIPSorcery mesh (signaling is host-owned) |
| `Novolis.Media.Capture.Windows` | Webcam capture for Windows |

Presentation stays in `Novolis.Avalonia.Media` (`VideoSurface`).

## Build

```powershell
dotnet build d:\novolis\novolis-video\Novolis.Video.slnx
dotnet pack d:\novolis\novolis-video\Novolis.Video.slnx -c Release
```

Local cross-repo dogfood (before GPR publish):

```powershell
dotnet build d:\novolis\novolis-dogfooding\apps\avalonia\ChannelLab\ChannelLab.csproj -p:NovolisUseProjectReferences=true
```

## Non-goals

LiveKit, Coturn, SFU, browser WebView / HTML WebRTC.
