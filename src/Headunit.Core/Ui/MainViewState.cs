using Headunit.Core.Playback;

namespace Headunit.Core.Ui;

/// <summary>
/// Everything the main window displays, as plain data. The shell copies the
/// player into this each frame; the renderer reads only this. Keeping the
/// renderer away from the live player is what makes a frame reproducible.
/// </summary>
public sealed class MainViewState
{
    public static readonly TimeSpan DefaultMessageDuration = TimeSpan.FromSeconds(1.5);

    public PlaybackState State { get; set; }

    public TimeSpan Position { get; set; }

    public TimeSpan Duration { get; set; }

    public bool ShowRemaining { get; set; }

    public float Volume { get; set; } = 1f;

    public float Balance { get; set; }

    public bool Shuffle { get; set; }

    public RepeatMode Repeat { get; set; }

    public StreamInfo? Info { get; set; }

    /// <summary>Whether the window has focus; the title bar dims when it doesn't.</summary>
    public bool IsActive { get; set; } = true;

    public string Title { get; private set; } = string.Empty;

    /// <summary>When the current title was first shown; the marquee scrolls from here.</summary>
    public TimeSpan TitleSince { get; private set; }

    public bool CanSeek => Duration > TimeSpan.Zero;

    public double SeekFraction
        => CanSeek ? Math.Clamp(Position.TotalMilliseconds / Duration.TotalMilliseconds, 0, 1) : 0;

    private string? _message;
    private TimeSpan _messageUntil;

    /// <summary>Sets the marquee text, restarting the scroll only if it actually changed.</summary>
    public void SetTitle(string text, TimeSpan now)
    {
        if (!string.Equals(text, Title, StringComparison.Ordinal))
        {
            Title = text;
            TitleSince = now;
        }
    }

    /// <summary>Briefly replaces the title — "REPEAT: ALL", "CANNOT PLAY X.MP3".</summary>
    public void ShowMessage(string text, TimeSpan now, TimeSpan? duration = null)
    {
        _message = text;
        _messageUntil = now + (duration ?? DefaultMessageDuration);
    }

    public string? MessageAt(TimeSpan now) => _message is not null && now < _messageUntil ? _message : null;
}
