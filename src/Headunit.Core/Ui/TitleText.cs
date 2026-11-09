using Headunit.Core.Playback;

namespace Headunit.Core.Ui;

public static class TitleText
{
    /// <summary>"3. Song name (4:05)" — the classic marquee line.</summary>
    public static string Compose(int queueIndex, Track track, TimeSpan duration)
    {
        var length = duration > TimeSpan.Zero ? duration : track.Duration ?? TimeSpan.Zero;
        var text = $"{queueIndex + 1}. {track.Title}";
        return length > TimeSpan.Zero ? $"{text} ({TimeDisplay.Format(length, length, showRemaining: false)})" : text;
    }

    public static string Volume(float volume) => $"VOLUME: {(int)Math.Round(volume * 100)}%";

    public static string Balance(float balance)
    {
        var percent = (int)Math.Round(Math.Abs(balance) * 100);
        return percent == 0 ? "BALANCE: CENTER" : $"BALANCE: {percent}% {(balance < 0 ? "LEFT" : "RIGHT")}";
    }

    public static string SeekTo(double fraction, TimeSpan duration)
    {
        var target = TimeSpan.FromTicks((long)(duration.Ticks * Math.Clamp(fraction, 0, 1)));
        var clock = TimeDisplay.Format(target, duration, showRemaining: false);
        var total = TimeDisplay.Format(duration, duration, showRemaining: false);
        return $"SEEK TO: {clock}/{total} ({(int)Math.Round(fraction * 100)}%)";
    }

    public static string Repeat(RepeatMode mode) => mode switch
    {
        RepeatMode.All => "REPEAT: ALL",
        RepeatMode.One => "REPEAT: ONE TRACK",
        _ => "REPEAT: OFF",
    };

    public static string Shuffle(bool on) => on ? "SHUFFLE: ON" : "SHUFFLE: OFF";
}
