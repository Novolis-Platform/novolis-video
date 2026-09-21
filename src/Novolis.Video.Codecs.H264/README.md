# Novolis.Video.Codecs.H264

Windows Media Foundation H.264 encoding and decoding for fixed-size video
streams. The encoder accepts BGRA32 frames and the decoder returns BGRA32
frames.

The Windows H.264 MFT is supplied by the operating system. A host should
advertise the capability only after constructing and initializing the codec.

## Install

```xml
<PackageReference Include="Novolis.Video.Codecs.H264" Version="2026.1.*" />
```

## Usage

Create `WindowsH264Encoder` and `WindowsH264Decoder` with the negotiated
stream dimensions and frame rate.
