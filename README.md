# novolis-video

Realtime video and a minimal Movie Maker–style edit core for the Novolis platform.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame`, `IRtcMeshSession`, signal DTOs |
| `Novolis.Video.Rtc` | SIPSorcery mesh (signaling is host-owned) |
| `Novolis.Video.Capture.Windows` | Webcam capture for Windows |
| `Novolis.Video.Edit` | Collections, storyboard clips, transport, solid-color preview |

Presentation stays in `Novolis.Avalonia.Video` (`VideoSurface`, storyboard strip, preview session). Dogfood: `novolis-dogfooding/apps/avalonia/MovieMakerLab`.

## Build

```powershell
dotnet build d:\novolis\novolis-video\Novolis.Video.slnx
dotnet test d:\novolis\novolis-video\tests\Novolis.Video.Edit.Unit\Novolis.Video.Edit.Unit.csproj
dotnet pack d:\novolis\novolis-video\Novolis.Video.slnx -c Release
```

Local cross-repo dogfood (before GPR publish):

```powershell
dotnet run --project d:\novolis\novolis-dogfooding\apps\avalonia\MovieMakerLab\MovieMakerLab.csproj -p:NovolisUseProjectReferences=true
```

## Non-goals

LiveKit, Coturn, SFU, browser WebView / HTML WebRTC, FFmpeg export (host-owned later).
