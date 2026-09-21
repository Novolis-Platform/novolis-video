# Novolis.Video.Abstractions

Platform-neutral contracts for desktop capture, encoded video access units,
and video codecs.

This package intentionally does not reference Avalonia, RTC, SIPSorcery, or
an operating-system capture API.

## Install

```xml
<PackageReference Include="Novolis.Video.Abstractions" Version="2026.1.*" />
```

## Usage

Implement `IVideoCaptureSource` for a platform capture source and exchange
frames through the codec contracts.
