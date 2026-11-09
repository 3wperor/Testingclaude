namespace Headunit.Core.Ui;

/// <summary>
/// What the four seven-segment digits show. Under 100 minutes it is MM:SS like
/// any CD unit; past that the readout switches to H:MM rather than lying.
/// </summary>
public readonly record struct LcdClock(bool Negative, int? D0, int? D1, int D2, int D3, bool Hours)
{
    public static LcdClock From(TimeSpan position, TimeSpan duration, bool showRemaining)
    {
        var value = showRemaining ? duration - position : position;
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        var totalSeconds = (long)value.TotalSeconds;
        var minutes = totalSeconds / 60;

        int high, low;
        bool hours = minutes >= 100;
        if (hours)
        {
            high = (int)Math.Min(minutes / 60, 99);
            low = (int)(minutes % 60);
        }
        else
        {
            high = (int)minutes;
            low = (int)(totalSeconds % 60);
        }

        // A leading zero on the minutes is left unlit — "4:05", not "04:05".
        int? d0 = high >= 10 ? high / 10 : null;
        return new LcdClock(showRemaining, d0, high % 10, low / 10, low % 10, hours);
    }
}
