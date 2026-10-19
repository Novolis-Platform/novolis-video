# Novolis.Video.Abstractions

Platform-neutral contracts for capture, encoded video access units, video
codecs, letterbox fit, stream gating, pipeline metrics, and progressive
random-access media.

This package intentionally does not reference Avalonia, RTC, SIPSorcery, or
an operating-system capture API.

## Install

```xml
<PackageReference Include="Novolis.Video.Abstractions" Version="2026.1.*" />
```

## Usage

Implement `IVideoCaptureSource` for a platform capture source and exchange
frames through the codec contracts.

For a file that is still arriving, implement `IProgressiveByteSource`. Reads
wait for missing ranges and `RequestRange` lets a transport prioritize the
container header and the next playback window. Android playback adapters use
this contract for P2P and torrent sources.
