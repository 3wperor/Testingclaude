using Headunit.Core.Playback;
using NAudio.CoreAudioApi;
using NAudio.Wave;

// NAudio ships its own PlaybackState; ours is the one the rest of the app means.
using PlaybackState = Headunit.Core.Playback.PlaybackState;

namespace Headunit.Audio;

/// <summary>
/// WASAPI shared-mode output, via NAudio.
/// <para>
/// The graph is <c>AudioFileReader -> BalanceSampleProvider -> WasapiOut</c>.
/// The equaliser, preamp, soft clip and FFT tap slot in between the reader and
/// the balance stage at M3 — the interface above does not change when they do.
/// </para>
/// </summary>
public sealed class NAudioEngine : IAudioEngine
{
    /// <summary>Shared mode, so we coexist with every other app's audio.</summary>
    private const int LatencyMilliseconds = 100;

    private WasapiOut? _output;
    private AudioFileReader? _reader;
    private BalanceSampleProvider? _balanceStage;
    private string? _path;
    private float _volume = 1f;
    private float _balance;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public TimeSpan Position
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_reader is not null)
            {
                _reader.CurrentTime = value;
            }
        }
    }

    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_reader is not null)
            {
                _reader.Volume = _volume;
            }
        }
    }

    public float Balance
    {
        get => _balance;
        set
        {
            _balance = Math.Clamp(value, -1f, 1f);
            if (_balanceStage is not null)
            {
                _balanceStage.Balance = _balance;
            }
        }
    }

    public StreamInfo? Info { get; private set; }

    public event EventHandler? PlaybackEnded;

    public event EventHandler<PlaybackFailedEventArgs>? PlaybackFailed;

    public void Load(string path)
    {
        TearDown();
        _path = path;

        try
        {
            // AudioFileReader picks a decoder by extension and falls back to
            // Media Foundation, which covers MP3/AAC/M4A/WMA/WAV and FLAC on
            // Windows 10. Ogg and Opus need their own decoders; they are wired
            // up with the rest of the format work, not here.
            _reader = new AudioFileReader(path) { Volume = _volume };
            _balanceStage = new BalanceSampleProvider(_reader) { Balance = _balance };
            Info = new StreamInfo(
                _reader.WaveFormat.SampleRate,
                _reader.WaveFormat.Channels,
                StreamInfo.EstimateKbps(new FileInfo(path).Length, _reader.TotalTime));
        }
        catch (Exception ex)
        {
            _reader = null;
            _balanceStage = null;
            Info = null;
            State = PlaybackState.Stopped;
            PlaybackFailed?.Invoke(this, new PlaybackFailedEventArgs(path, ex));
            return;
        }

        State = PlaybackState.Stopped;
    }

    public void Play()
    {
        if (_reader is null || _balanceStage is null)
        {
            return;
        }

        try
        {
            if (_output is null)
            {
                // Shared mode tolerates being created on the STA UI thread.
                // The exclusive/bit-perfect path, if we ever take it, must move
                // this onto an MTA thread.
                _output = new WasapiOut(AudioClientShareMode.Shared, useEventSync: true, LatencyMilliseconds);
                _output.PlaybackStopped += OnPlaybackStopped;
                _output.Init(_balanceStage);
            }

            _output.Play();
            State = PlaybackState.Playing;
        }
        catch (Exception ex)
        {
            var path = _path ?? string.Empty;
            TearDown();
            PlaybackFailed?.Invoke(this, new PlaybackFailedEventArgs(path, ex));
        }
    }

    public void Pause()
    {
        if (_output is null || State != PlaybackState.Playing)
        {
            return;
        }

        _output.Pause();
        State = PlaybackState.Paused;
    }

    public void Stop()
    {
        // Detaching the handler first is what makes a deliberate stop
        // distinguishable from a track ending: NAudio raises PlaybackStopped
        // for both, and only the latter may advance the queue.
        DetachOutput();

        if (_reader is not null)
        {
            _reader.Position = 0;
        }

        State = PlaybackState.Stopped;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        State = PlaybackState.Stopped;

        if (e.Exception is not null)
        {
            PlaybackFailed?.Invoke(this, new PlaybackFailedEventArgs(_path ?? string.Empty, e.Exception));
            return;
        }

        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private void DetachOutput()
    {
        if (_output is null)
        {
            return;
        }

        _output.PlaybackStopped -= OnPlaybackStopped;
        _output.Stop();
        _output.Dispose();
        _output = null;
    }

    private void TearDown()
    {
        DetachOutput();
        _reader?.Dispose();
        _reader = null;
        _balanceStage = null;
        Info = null;
    }

    public void Dispose() => TearDown();
}
