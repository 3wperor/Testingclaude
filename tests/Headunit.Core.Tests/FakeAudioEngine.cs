using Headunit.Core.Playback;

namespace Headunit.Core.Tests;

/// <summary>Stand-in for WASAPI so the controller's decisions can be tested.</summary>
internal sealed class FakeAudioEngine : IAudioEngine
{
    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public TimeSpan Position { get; set; }

    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(3);

    public float Volume { get; set; } = 1f;

    public string? LoadedPath { get; private set; }

    public List<string> LoadHistory { get; } = [];

    public event EventHandler? PlaybackEnded;

    public event EventHandler<PlaybackFailedEventArgs>? PlaybackFailed;

    public void Load(string path)
    {
        LoadedPath = path;
        LoadHistory.Add(path);
        Position = TimeSpan.Zero;
        State = PlaybackState.Stopped;
    }

    public void Play() => State = PlaybackState.Playing;

    public void Pause() => State = PlaybackState.Paused;

    public void Stop()
    {
        State = PlaybackState.Stopped;
        Position = TimeSpan.Zero;
    }

    public void RaiseEnded() => PlaybackEnded?.Invoke(this, EventArgs.Empty);

    public void RaiseFailed(string path)
        => PlaybackFailed?.Invoke(this, new PlaybackFailedEventArgs(path, new IOException("boom")));

    public void Dispose()
    {
    }
}
