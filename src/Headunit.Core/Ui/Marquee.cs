namespace Headunit.Core.Ui;

/// <summary>
/// The scrolling title. Text that fits sits still; text that doesn't holds for
/// a moment so it can be read from the start, then scrolls continuously,
/// wrapping through a separator.
/// </summary>
public static class Marquee
{
    public const string Separator = "  ***  ";

    public static readonly TimeSpan Hold = TimeSpan.FromSeconds(1.5);

    public static readonly TimeSpan Step = TimeSpan.FromMilliseconds(40);

    /// <summary>
    /// Pixel offset to draw the text at, scrolled left. <paramref name="period"/>
    /// is text width plus separator width: the distance after which the
    /// picture repeats.
    /// </summary>
    public static int Offset(int textWidth, int fieldWidth, int period, TimeSpan sinceShown)
    {
        if (textWidth <= fieldWidth || period <= 0 || sinceShown <= Hold)
        {
            return 0;
        }

        var steps = (long)((sinceShown - Hold).Ticks / Step.Ticks);
        return (int)(steps % period);
    }
}
