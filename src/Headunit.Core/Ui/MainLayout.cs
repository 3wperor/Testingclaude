using Headunit.Core.Graphics;
using Headunit.Core.Layout;

namespace Headunit.Core.Ui;

/// <summary>Everything on the main window you can press, drag or read.</summary>
public enum MainWidget
{
    None,
    TitleBar,
    Minimize,
    Close,
    TimeReadout,
    Visualizer,
    Volume,
    Balance,
    EqToggle,
    PlaylistToggle,
    Seek,
    Previous,
    Play,
    Pause,
    Stop,
    Next,
    Eject,
    Shuffle,
    Repeat,
}

/// <summary>
/// Where everything sits on the 275x116 main window, in art pixels. Proportions
/// follow the classic 2.x layout so it feels right in the hand; the artwork
/// drawn into these boxes is our own.
/// </summary>
public static class MainLayout
{
    public static readonly PixelRect Window = new(0, 0, WindowMetrics.MainWidth, WindowMetrics.MainHeight);

    // ---- Title bar ---------------------------------------------------------
    public static readonly PixelRect TitleBar = new(0, 0, WindowMetrics.MainWidth, WindowMetrics.TitleBarHeight);
    public static readonly PixelRect Minimize = new(254, 3, 9, 9);
    public static readonly PixelRect Close = new(264, 3, 9, 9);

    // ---- Left LCD: status, clock, indicators, visualiser ------------------
    public static readonly PixelRect LeftLcd = new(9, 18, 97, 45);
    public static readonly PixelRect Status = new(14, 26, 9, 9);
    public static readonly PixelRect Sign = new(26, 24, 9, 13);
    public static readonly PixelRect[] Digits =
    [
        new(37, 24, 9, 13),
        new(48, 24, 9, 13),
        new(63, 24, 9, 13),
        new(74, 24, 9, 13),
    ];

    public static readonly PixelRect Colon = new(59, 24, 2, 13);
    public static readonly PixelRect TimeReadout = new(24, 22, 61, 17);
    public static readonly (int X, int Y) RandomIndicator = (88, 25);
    public static readonly (int X, int Y) RepeatIndicator = (88, 32);
    public static readonly (int X, int Y) RepeatOneIndicator = (100, 32);

    public static readonly PixelRect Visualizer = new(
        15, 43, WindowMetrics.VisualizerWidth, WindowMetrics.VisualizerHeight);

    // ---- Right LCD: title marquee, stream info ----------------------------
    public static readonly PixelRect RightLcd = new(108, 18, 159, 35);
    public static readonly PixelRect TitleField = new(112, 24, 154 - 2, 7);
    public static readonly (int X, int Y) Kbps = (112, 39);
    public static readonly (int X, int Y) KbpsLabel = (137, 41);
    public static readonly (int X, int Y) Khz = (158, 39);
    public static readonly (int X, int Y) KhzLabel = (177, 41);
    public static readonly (int X, int Y) MonoLabel = (211, 41);
    public static readonly (int X, int Y) StereoLabel = (239, 41);

    // ---- Sliders and window toggles ---------------------------------------
    public static readonly PixelRect Volume = new(107, 57, 68, 13);
    public static readonly PixelRect Balance = new(177, 57, 38, 13);
    public const int SmallThumbWidth = 14;
    public const int SmallThumbHeight = 11;

    public static readonly PixelRect EqToggle = new(219, 58, 23, 12);
    public static readonly PixelRect PlaylistToggle = new(242, 58, 23, 12);

    public static readonly PixelRect Seek = new(16, 72, 248, 10);
    public const int SeekThumbWidth = 29;

    // ---- Transport ---------------------------------------------------------
    public static readonly PixelRect Previous = new(16, 88, 23, 18);
    public static readonly PixelRect Play = new(39, 88, 23, 18);
    public static readonly PixelRect Pause = new(62, 88, 23, 18);
    public static readonly PixelRect Stop = new(85, 88, 23, 18);
    public static readonly PixelRect Next = new(108, 88, 22, 18);
    public static readonly PixelRect Eject = new(136, 89, 22, 16);
    public static readonly PixelRect Shuffle = new(163, 89, 46, 15);
    public static readonly PixelRect Repeat = new(211, 89, 28, 15);
    public static readonly PixelRect Logo = new(250, 90, 16, 16);

    public static SliderGeometry VolumeSlider { get; } = new(Volume, SmallThumbWidth);

    public static SliderGeometry BalanceSlider { get; } = new(Balance, SmallThumbWidth);

    public static SliderGeometry SeekSlider { get; } = new(Seek, SeekThumbWidth);

    /// <summary>Hit targets, most specific first — title bar buttons before the bar itself.</summary>
    public static IReadOnlyList<(MainWidget Widget, PixelRect Rect)> HitTargets { get; } =
    [
        (MainWidget.Minimize, Minimize),
        (MainWidget.Close, Close),
        (MainWidget.TitleBar, TitleBar),
        (MainWidget.TimeReadout, TimeReadout),
        (MainWidget.Visualizer, Visualizer),
        (MainWidget.Volume, Volume),
        (MainWidget.Balance, Balance),
        (MainWidget.EqToggle, EqToggle),
        (MainWidget.PlaylistToggle, PlaylistToggle),
        (MainWidget.Seek, Seek),
        (MainWidget.Previous, Previous),
        (MainWidget.Play, Play),
        (MainWidget.Pause, Pause),
        (MainWidget.Stop, Stop),
        (MainWidget.Next, Next),
        (MainWidget.Eject, Eject),
        (MainWidget.Shuffle, Shuffle),
        (MainWidget.Repeat, Repeat),
    ];

    public static MainWidget HitTest(int x, int y)
    {
        foreach (var (widget, rect) in HitTargets)
        {
            if (rect.Contains(x, y))
            {
                return widget;
            }
        }

        return MainWidget.None;
    }

    public static PixelRect RectOf(MainWidget widget)
    {
        foreach (var (w, rect) in HitTargets)
        {
            if (w == widget)
            {
                return rect;
            }
        }

        return default;
    }
}

/// <summary>A horizontal slider: a track and a thumb that travels inside it.</summary>
public readonly record struct SliderGeometry(PixelRect Track, int ThumbWidth)
{
    /// <summary>How far the thumb's left edge can travel, in pixels.</summary>
    public int Travel => Track.Width - ThumbWidth;

    /// <summary>Thumb left edge, in window coordinates, for a 0..1 fraction.</summary>
    public int ThumbX(double fraction)
        => Track.X + (int)Math.Round(Math.Clamp(fraction, 0, 1) * Travel, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The fraction that puts the thumb under the pointer, given where on the
    /// thumb it was grabbed.
    /// </summary>
    public double FractionAt(int pointerX, int grabOffset)
        => Travel <= 0 ? 0 : Math.Clamp((pointerX - grabOffset - Track.X) / (double)Travel, 0, 1);

    public bool IsOnThumb(int pointerX, double fraction)
    {
        var x = ThumbX(fraction);
        return pointerX >= x && pointerX < x + ThumbWidth;
    }
}
