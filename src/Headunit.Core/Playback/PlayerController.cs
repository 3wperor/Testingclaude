namespace Headunit.Core.Playback;

/// <summary>
/// Binds the queue to the audio engine: what happens when a track ends, what
/// Previous means, and which failures should skip rather than stop.
/// </summary>
public sealed class PlayerController : IDisposable
{
    private readonly IAudioEngine _engine;

    public PlayerController(IAudioEngine engine, PlayQueue? queue = null)
    {
        _engine = engine;
        Queue = queue ?? new PlayQueue();
        _engine.PlaybackEnded += OnPlaybackEnded;
        _engine.PlaybackFailed += OnPlaybackFailed;
    }

    public PlayQueue Queue { get; }

    public PlaybackState State => _engine.State;

    public TimeSpan Position => _engine.Position;

    public TimeSpan Duration => _engine.Duration;

    public float Volume
    {
        get => _engine.Volume;
        set => _engine.Volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// How far into a track Previous restarts it instead of going back one.
    /// Every physical transport ever made behaves this way.
    /// </summary>
    public TimeSpan RestartThreshold { get; set; } = TimeSpan.FromSeconds(3);

    public event EventHandler? TrackChanged;

    public event EventHandler? StateChanged;

    /// <summary>Raised when a track could not be played and was skipped.</summary>
    public event EventHandler<PlaybackFailedEventArgs>? TrackFailed;

    public void PlayAt(int trackIndex)
    {
        if (Queue.MoveTo(trackIndex))
        {
            StartCurrent();
        }
    }

    public void TogglePlayPause()
    {
        switch (_engine.State)
        {
            case PlaybackState.Playing:
                _engine.Pause();
                break;

            case PlaybackState.Paused:
                _engine.Play();
                break;

            case PlaybackState.Stopped:
                if (Queue.Current is not null)
                {
                    StartCurrent();
                    return;
                }

                break;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        _engine.Stop();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Next() => Advance(userInitiated: true);

    public void Previous()
    {
        // Past the threshold, Previous means "start this one again".
        if (_engine.State != PlaybackState.Stopped && _engine.Position > RestartThreshold)
        {
            Seek(TimeSpan.Zero);
            return;
        }

        if (Queue.MovePrevious())
        {
            StartCurrent();
        }
        else
        {
            Seek(TimeSpan.Zero);
        }
    }

    public void Seek(TimeSpan position)
    {
        var clamped = position < TimeSpan.Zero ? TimeSpan.Zero : position;
        var duration = _engine.Duration;

        if (duration > TimeSpan.Zero && clamped > duration)
        {
            clamped = duration;
        }

        _engine.Position = clamped;
    }

    private void Advance(bool userInitiated)
    {
        if (Queue.MoveNext(userInitiated))
        {
            StartCurrent();
        }
        else
        {
            // End of queue with repeat off: stop, but keep the cursor where it
            // is so pressing play again resumes from the last track.
            Stop();
        }
    }

    private void StartCurrent()
    {
        var track = Queue.Current;
        if (track is null)
        {
            return;
        }

        _engine.Load(track.Path);
        _engine.Play();

        TrackChanged?.Invoke(this, EventArgs.Empty);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPlaybackEnded(object? sender, EventArgs e) => Advance(userInitiated: false);

    private void OnPlaybackFailed(object? sender, PlaybackFailedEventArgs e)
    {
        // A missing or corrupt file must not wedge the queue: report it and
        // move on, the same as a natural end.
        TrackFailed?.Invoke(this, e);
        Advance(userInitiated: false);
    }

    public void Dispose()
    {
        _engine.PlaybackEnded -= OnPlaybackEnded;
        _engine.PlaybackFailed -= OnPlaybackFailed;
        _engine.Dispose();
    }
}
