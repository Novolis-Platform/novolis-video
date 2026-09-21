<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-video">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Video.Rtc

SIPSorcery mesh RTC implementing `IRtcMeshSession`. Signaling is application-owned (e.g. SignalR).

## Install

```bash
dotnet add package Novolis.Video.Rtc
```

## Quick start

```csharp
await using var session = new SipSorceryRtcMeshSession("alice");
session.LocalSignal += msg => /* relay via SignalR */;
session.RemoteFrame += (nick, frame) => /* VideoSurface */;
session.AudioError += error => /* show microphone or speaker status */;
await session.JoinVideoAsync();
session.SetMuted(true); // local mute; remote audio continues
```

On Windows, the session captures webcam video and microphone audio through
`SIPSorceryMedia.Windows`, sends VP8 and WebRTC audio tracks to each mesh peer,
and plays received audio through the same Windows audio endpoint. Polite peer =
lexicographically greater nick. Max peers: `IRtcMeshSession.MaxPeers` (4).

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | Contracts |
| `Novolis.Video.Capture.Windows` | Webcam |
| `Novolis.Avalonia.Video` | UI surface |

