namespace Headunit.Core.Playback;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
}

/// <summary>
/// The platform's audio output, seen from the engine's side of the fence.
/// Everything above this interface is portable; only the implementation is
/// Windows-specific, which is what lets the controller be unit-tested and the
/// macOS port reuse the design.
/// </summary>
public interface IAudioEngine : IDisposable
{
    PlaybackState State { get; }

    TimeSpan Position { get; set; }

    TimeSpan Duration { get; }

    /// <summary>Linear 0.0 to 1.0.</summary>
    float Volume { get; set; }

    /// <summary>-1 (left) to +1 (right); applied with <see cref="BalanceLaw"/>.</summary>
    float Balance { get; set; }

    /// <summary>Format of the loaded file, or null when nothing is loaded.</summary>
    StreamInfo? Info { get; }

    void Load(string path);

    void Play();

    void Pause();

    void Stop();

    /// <summary>
    /// Raised when a track reaches its end on its own. Deliberately NOT raised
    /// by <see cref="Stop"/> — otherwise stopping would advance the queue.
    /// </summary>
    event EventHandler? PlaybackEnded;

    /// <summary>Raised when the output device or decoder fails mid-stream.</summary>
    event EventHandler<PlaybackFailedEventArgs>? PlaybackFailed;
}

public sealed class PlaybackFailedEventArgs(string path, Exception exception) : EventArgs
{
    public string Path { get; } = path;

    public Exception Exception { get; } = exception;
}
