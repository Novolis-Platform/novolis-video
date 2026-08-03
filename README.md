<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-video.svg" width="100%" alt="novolis-video"/>
</p>

<p align="center">
  <strong>RTC mesh and movie edit core</strong><br/>
  Realtime video RTC contracts/mesh, Windows capture, and storyboard edit core.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-video/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-video/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-video"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Video.Capture.Windows` | `dotnet add package Novolis.Video.Capture.Windows` | [README](https://github.com/Novolis-Platform/novolis-video/blob/main/src/Novolis.Video.Capture.Windows/README.md) |
| `Novolis.Video.Edit` | `dotnet add package Novolis.Video.Edit` | [README](https://github.com/Novolis-Platform/novolis-video/blob/main/src/Novolis.Video.Edit/README.md) |
| `Novolis.Video.Rtc` | `dotnet add package Novolis.Video.Rtc` | [README](https://github.com/Novolis-Platform/novolis-video/blob/main/src/Novolis.Video.Rtc/README.md) |
| `Novolis.Video.Rtc.Abstractions` | `dotnet add package Novolis.Video.Rtc.Abstractions` | [README](https://github.com/Novolis-Platform/novolis-video/blob/main/src/Novolis.Video.Rtc.Abstractions/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->
# novolis-video

Realtime video and a minimal Movie Maker–style edit core for the Novolis platform.

## Packages

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame`, `IRtcMeshSession`, signal DTOs |
| `Novolis.Video.Rtc` | SIPSorcery mesh (signaling is host-owned) |
| `Novolis.Video.Capture.Windows` | Webcam capture for Windows |
| `Novolis.Video.Edit` | Storyboard, audio track, transitions, text overlays, BMP+WAV export |

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

