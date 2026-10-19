using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Views;
using Android.Widget;
using Novolis.Video;
using AndroidUri = Android.Net.Uri;

namespace Novolis.Video.Playback.Android;

/// <summary>
/// A composable native Android video surface with URI and progressive-source
/// inputs.
/// </summary>
/// <remarks>
/// This view deliberately does not create media notifications, publish media
/// sessions, or retain a title, URI, or playback history. The host owns the
/// surrounding controls and decides whether an external player is offered.
/// </remarks>
public sealed class AndroidVideoPlayerView : FrameLayout
{
    private const float MinimumZoom = 1f;
    private const float MaximumZoom = 4f;

    private readonly Matrix _videoTransform = new();
    private readonly TextureView _texture;
    private ProgressiveMediaDataSource? _dataSource;
    private MediaPlayer? _player;
    private IProgressiveByteSource? _progressiveSource;
    private Surface? _surface;
    private AndroidUri? _uri;
    private bool _disposed;
    private bool _pinching;
    private bool _shouldPlay;
    private float _pinchStartDistance;
    private float _pinchStartZoom = MinimumZoom;
    private float _zoom = MinimumZoom;
    private int _videoHeight;
    private int _videoWidth;

    /// <summary>
    /// Initializes the player view.
    /// </summary>
    public AndroidVideoPlayerView(Context context)
        : base(context)
    {
        _texture = new TextureView(context)
        {
            ContentDescription = "Video playback surface; pinch to zoom",
        };
        _texture.SurfaceTextureAvailable += OnSurfaceTextureAvailable;
        _texture.SurfaceTextureDestroyed += OnSurfaceTextureDestroyed;
        _texture.Touch += OnTextureTouch;
        AddView(
            _texture,
            new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent));
        SetBackgroundColor(Color.Black);
    }

    /// <summary>Raised after the source has been prepared.</summary>
    public event EventHandler? Prepared;

    /// <summary>Raised when native preparation or playback fails.</summary>
    public event EventHandler<PlaybackFailureEventArgs>? PlaybackFailed;

    /// <summary>Raised when the current source reaches its end.</summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>Raised when playback starts.</summary>
    public event EventHandler? PlaybackStarted;

    /// <summary>Raised when playback is paused.</summary>
    public event EventHandler? PlaybackPaused;

    /// <summary>Gets the video surface that hosts may place in custom layouts.</summary>
    public TextureView VideoSurface => _texture;

    /// <summary>Gets the media duration in milliseconds.</summary>
    public int DurationMilliseconds { get; private set; }

    /// <summary>Gets the current playback position in milliseconds.</summary>
    public int CurrentPositionMilliseconds
    {
        get
        {
            try
            {
                return _player?.CurrentPosition ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>Gets whether a source has been assigned.</summary>
    public bool HasSource => _uri is not null || _progressiveSource is not null;

    /// <summary>Gets whether the source is prepared.</summary>
    public bool IsPrepared => _player is not null && _prepared;

    /// <summary>Gets whether native playback is active.</summary>
    public bool IsPlaying
    {
        get
        {
            try
            {
                return _player?.IsPlaying == true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Gets or sets the desired playback state across surface recreation.</summary>
    public bool ShouldPlay
    {
        get => _shouldPlay;
        set => _shouldPlay = value;
    }

    /// <summary>Gets or sets the fit mode before gesture zoom is applied.</summary>
    public AndroidVideoScaleMode ScaleMode { get; set; } = AndroidVideoScaleMode.FitView;

    /// <summary>
    /// Replaces the source with a content, file, or network URI.
    /// </summary>
    public void Open(AndroidUri uri, bool autoPlay = true)
    {
        ArgumentNullException.ThrowIfNull(uri);
        EnsureNotDisposed();
        ReleasePlayer();
        DisposeProgressiveSource();
        _uri = uri;
        _shouldPlay = autoPlay;
        ResetTransform();
        TryStartPlayback();
    }

    /// <summary>
    /// Replaces the source with a progressive random-access source.
    /// </summary>
    /// <remarks>
    /// The view owns this source after the call and disposes it when a new
    /// source is opened or the view is released.
    /// </remarks>
    public void Open(IProgressiveByteSource source, bool autoPlay = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        EnsureNotDisposed();
        ReleasePlayer();
        DisposeProgressiveSource();
        _uri = null;
        _progressiveSource = source;
        _shouldPlay = autoPlay;
        ResetTransform();
        TryStartPlayback();
    }

    /// <summary>Retries the current source after a transient failure.</summary>
    public void Retry()
    {
        EnsureNotDisposed();
        if (!HasSource)
            return;

        ReleasePlayer();
        TryStartPlayback();
    }

    /// <summary>Stops native playback and clears the current source.</summary>
    public void ClearSource()
    {
        EnsureNotDisposed();
        ReleasePlayer();
        DisposeProgressiveSource();
        _uri = null;
        _shouldPlay = false;
        ResetTransform();
    }

    /// <summary>Starts or resumes playback.</summary>
    public void Play()
    {
        EnsureNotDisposed();
        _shouldPlay = true;
        if (_player is null || !_prepared)
        {
            TryStartPlayback();
            return;
        }

        try
        {
            if (DurationMilliseconds > 0
                && CurrentPositionMilliseconds >= DurationMilliseconds)
            {
                _player.SeekTo(0);
            }

            _player.Start();
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            RaisePlaybackFailure(duringPreparation: false);
        }
    }

    /// <summary>Pauses playback and keeps the source assigned.</summary>
    public void Pause()
    {
        EnsureNotDisposed();
        _shouldPlay = false;
        try
        {
            if (_player?.IsPlaying == true)
                _player.Pause();
        }
        catch
        {
        }

        PlaybackPaused?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Seeks to a position in the current source.</summary>
    public void SeekTo(int positionMilliseconds)
    {
        if (!IsPrepared)
            return;

        try
        {
            _player!.SeekTo(Math.Max(0, positionMilliseconds));
        }
        catch
        {
        }
    }

    /// <summary>Sets the left and right media volume.</summary>
    public void SetVolume(float volume)
    {
        var clamped = Math.Clamp(volume, 0f, 1f);
        try
        {
            _player?.SetVolume(clamped, clamped);
        }
        catch
        {
        }
    }

    /// <summary>Sets the gesture zoom and refreshes the transformed frame.</summary>
    public void SetZoom(float zoom)
    {
        _zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
        ApplyTransform();
    }

    /// <summary>Returns the gesture zoom to its neutral value.</summary>
    public void ResetZoom()
    {
        _zoom = MinimumZoom;
        ApplyTransform();
    }

    /// <summary>Refreshes the current fit and zoom transform after a layout change.</summary>
    public void RefreshTransform() => ApplyTransform();

    /// <summary>Releases native playback and the assigned progressive source.</summary>
    public void Release()
    {
        if (_disposed)
            return;

        _disposed = true;
        ReleasePlayer();
        DisposeProgressiveSource();
        _surface?.Release();
        _surface = null;
        _texture.SurfaceTextureAvailable -= OnSurfaceTextureAvailable;
        _texture.SurfaceTextureDestroyed -= OnSurfaceTextureDestroyed;
        _texture.Touch -= OnTextureTouch;
    }

    protected override void OnSizeChanged(int width, int height, int oldWidth, int oldHeight)
    {
        base.OnSizeChanged(width, height, oldWidth, oldHeight);
        ApplyTransform();
    }

    private bool _prepared;

    private void TryStartPlayback()
    {
        if (_disposed
            || !HasSource
            || _surface is null
            || _player is not null)
        {
            return;
        }

        MediaPlayer? player = null;
        try
        {
            player = new MediaPlayer();
            _player = player;
            player.Prepared += (_, _) => HandlePrepared(player);
            player.Completion += (_, _) =>
            {
                if (ReferenceEquals(_player, player))
                    PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            };
            player.Error += (_, _) =>
            {
                if (ReferenceEquals(_player, player))
                    RaisePlaybackFailure(!_prepared);
            };
            player.SetSurface(_surface);
            SetDataSource(player);
            player.PrepareAsync();
        }
        catch
        {
            if (player is not null)
                ReleasePlayer(player);
            RaisePlaybackFailure(duringPreparation: true);
        }
    }

    private void SetDataSource(MediaPlayer player)
    {
        if (_uri is not null)
        {
            player.SetDataSource(Context!, _uri);
            return;
        }

        if (_progressiveSource is not null)
        {
            _dataSource = new ProgressiveMediaDataSource(
                _progressiveSource,
                leaveSourceOpen: true);
            player.SetDataSource(_dataSource);
            return;
        }

        throw new InvalidOperationException("No media source is assigned.");
    }

    private void HandlePrepared(MediaPlayer player)
    {
        if (!ReferenceEquals(_player, player) || _disposed)
        {
            ReleasePlayer(player);
            return;
        }

        try
        {
            DurationMilliseconds = Math.Max(0, player.Duration);
            _videoWidth = Math.Max(0, player.VideoWidth);
            _videoHeight = Math.Max(0, player.VideoHeight);
            _prepared = true;
            ApplyTransform();
            Prepared?.Invoke(this, EventArgs.Empty);
            if (_shouldPlay)
            {
                player.Start();
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            RaisePlaybackFailure(duringPreparation: false);
        }
    }

    private void RaisePlaybackFailure(bool duringPreparation)
    {
        var source = _player;
        if (source is not null)
            ReleasePlayer(source);

        PlaybackFailed?.Invoke(
            this,
            new PlaybackFailureEventArgs(duringPreparation));
    }

    private void OnSurfaceTextureAvailable(
        object? sender,
        TextureView.SurfaceTextureAvailableEventArgs args)
    {
        if (_disposed)
            return;

        try
        {
            _surface?.Release();
            _surface = new Surface(args.Surface);
            ApplyTransform();
            TryStartPlayback();
        }
        catch
        {
            RaisePlaybackFailure(duringPreparation: true);
        }
    }

    private void OnSurfaceTextureDestroyed(
        object? sender,
        TextureView.SurfaceTextureDestroyedEventArgs args)
    {
        ReleasePlayer();
        _surface?.Release();
        _surface = null;
    }

    private void OnTextureTouch(object? sender, View.TouchEventArgs args)
    {
        if (args.Event is { } motionEvent)
        {
            switch (motionEvent.ActionMasked)
            {
                case MotionEventActions.Down:
                    _pinching = false;
                    _pinchStartDistance = 0f;
                    _pinchStartZoom = _zoom;
                    break;
                case MotionEventActions.PointerDown:
                    if (motionEvent.PointerCount >= 2)
                    {
                        _pinchStartDistance = GetPointerDistance(motionEvent);
                        _pinchStartZoom = _zoom;
                        _pinching = _pinchStartDistance > 0f;
                    }

                    break;
                case MotionEventActions.Move:
                    if (_pinching && motionEvent.PointerCount >= 2)
                    {
                        var distance = GetPointerDistance(motionEvent);
                        if (distance > 0f && _pinchStartDistance > 0f)
                        {
                            SetZoom(_pinchStartZoom * distance / _pinchStartDistance);
                        }
                    }

                    break;
                case MotionEventActions.PointerUp:
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _pinching = false;
                    _pinchStartDistance = 0f;
                    break;
            }
        }

        args.Handled = true;
    }

    private static float GetPointerDistance(MotionEvent motionEvent)
    {
        var deltaX = motionEvent.GetX(0) - motionEvent.GetX(1);
        var deltaY = motionEvent.GetY(0) - motionEvent.GetY(1);
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private void ApplyTransform()
    {
        if (_texture.Width <= 0 || _texture.Height <= 0)
            return;

        var viewWidth = _texture.Width;
        var viewHeight = _texture.Height;
        var scaleX = 1f;
        var scaleY = 1f;
        if (_videoWidth > 0 && _videoHeight > 0)
        {
            var videoAspect = (float)_videoWidth / _videoHeight;
            var viewAspect = (float)viewWidth / viewHeight;
            var targetWidth = (float)viewWidth;
            var targetHeight = (float)viewHeight;
            switch (ScaleMode)
            {
                case AndroidVideoScaleMode.FitWidth:
                    targetHeight = viewWidth / videoAspect;
                    break;
                case AndroidVideoScaleMode.FitHeight:
                    targetWidth = viewHeight * videoAspect;
                    break;
                default:
                    if (viewAspect > videoAspect)
                        targetWidth = viewHeight * videoAspect;
                    else
                        targetHeight = viewWidth / videoAspect;
                    break;
            }

            scaleX = targetWidth / viewWidth;
            scaleY = targetHeight / viewHeight;
        }

        _videoTransform.Reset();
        _videoTransform.SetScale(
            scaleX * _zoom,
            scaleY * _zoom,
            viewWidth / 2f,
            viewHeight / 2f);
        _texture.SetTransform(_videoTransform);
        _texture.Invalidate();
        _texture.PostInvalidateOnAnimation();
        Invalidate();
    }

    private void ResetTransform()
    {
        _videoWidth = 0;
        _videoHeight = 0;
        DurationMilliseconds = 0;
        _prepared = false;
        _zoom = MinimumZoom;
        ApplyTransform();
    }

    private void ReleasePlayer(MediaPlayer? player = null)
    {
        player ??= _player;
        if (player is null)
            return;

        var isCurrentPlayer = ReferenceEquals(_player, player);
        if (isCurrentPlayer)
        {
            _player = null;
            _prepared = false;
            DurationMilliseconds = 0;
        }

        try
        {
            player.Stop();
        }
        catch
        {
        }

        try
        {
            player.Reset();
        }
        catch
        {
        }

        try
        {
            player.Release();
        }
        catch
        {
        }

        if (isCurrentPlayer)
        {
            _dataSource?.Close();
            _dataSource = null;
        }
    }

    private void DisposeProgressiveSource()
    {
        _dataSource?.Close();
        _dataSource = null;
        if (_progressiveSource is not null)
        {
            _progressiveSource.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _progressiveSource = null;
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

