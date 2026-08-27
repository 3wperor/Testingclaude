namespace Headunit.Core.Playback;

/// <summary>
/// The LCD time readout. Clicking it toggles between elapsed and remaining,
/// and remaining is shown with a leading minus — a detail people notice
/// immediately if it is missing.
/// </summary>
public static class TimeDisplay
{
    public static string Format(TimeSpan position, TimeSpan duration, bool showRemaining)
    {
        if (!showRemaining)
        {
            return FormatClock(position);
        }

        var remaining = duration - position;
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        return "-" + FormatClock(remaining);
    }

    private static string FormatClock(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        var totalMinutes = (int)value.TotalMinutes;

        // Past an hour the readout grows rather than wrapping back to 0:00.
        return totalMinutes >= 60
            ? $"{totalMinutes / 60}:{totalMinutes % 60:00}:{value.Seconds:00}"
            : $"{totalMinutes}:{value.Seconds:00}";
    }
}
