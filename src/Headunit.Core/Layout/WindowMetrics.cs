namespace Headunit.Core.Layout;

/// <summary>
/// Canonical window dimensions, in <em>art pixels</em> — the 1:1 units the
/// sprite atlas is drawn in. Nothing in the codebase may hard-code these
/// numbers; everything scales from here.
/// </summary>
public static class WindowMetrics
{
    public const int MainWidth = 275;
    public const int MainHeight = 116;

    /// <summary>Height of the main window collapsed to its title strip.</summary>
    public const int ShadeHeight = 14;

    public const int EqualizerWidth = 275;
    public const int EqualizerHeight = 116;

    public const int LibraryMinWidth = 275;
    public const int LibraryMinHeight = 116;

    /// <summary>
    /// The library/playlist window resizes in whole sprite tiles, never freely —
    /// that quantisation is a large part of why the classic UI never looks broken.
    /// </summary>
    public const int LibraryWidthStep = 25;
    public const int LibraryHeightStep = 29;

    public const int TitleBarHeight = 14;

    /// <summary>Edge distance, in art pixels, within which windows snap together.</summary>
    public const int SnapThreshold = 10;

    public const int VisualizerWidth = 76;
    public const int VisualizerHeight = 16;

    /// <summary>Bars in the spectrum display; fixed by the visualizer's art width.</summary>
    public const int SpectrumBars = 19;
}
