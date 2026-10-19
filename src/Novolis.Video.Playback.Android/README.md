# Novolis.Video.Playback.Android

Composable native Android playback primitives for Novolis applications.

`ProgressiveMediaDataSource` adapts `IProgressiveByteSource` to Android's
`MediaPlayer`. The media framework can request container and sample ranges out
of order; the adapter waits for a transport to supply each range instead of
mistaking an unwritten torrent chunk for corrupted media.

The package contains no notification, history, title, or media-library
integration. The host decides whether to expose a URI to another application.
