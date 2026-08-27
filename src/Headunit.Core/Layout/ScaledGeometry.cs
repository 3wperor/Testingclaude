namespace Headunit.Core.Layout;

public readonly record struct PixelSize(int Width, int Height);

public readonly record struct DipSize(double Width, double Height);

/// <summary>
/// Integer-scaling maths for a pixel-art UI on a DPI-scaled desktop.
/// <para>
/// The whole point: one art pixel must always cover a whole number of
/// <em>physical</em> pixels, so the nearest-neighbour blit stays crisp. WPF
/// lays out in device-independent units (1/96"), which the OS then multiplies
/// by the monitor's DPI scale — at 150% a 275 DIP window is 412.5 physical
/// pixels and the art smears. So we pre-divide by the DPI scale and let the
/// two cancel out.
/// </para>
/// </summary>
public static class ScaledGeometry
{
    public const int MinScale = 1;
    public const int MaxScale = 4;

    public static int ClampScale(int scale) => Math.Clamp(scale, MinScale, MaxScale);

    /// <summary>
    /// The WPF <c>LayoutTransform</c> factor that makes one art pixel cover
    /// exactly <paramref name="scale"/> physical pixels on a monitor whose DPI
    /// scale is <paramref name="dpiScale"/> (1.0 at 96 DPI, 1.5 at 150%).
    /// </summary>
    public static double LayoutScale(int scale, double dpiScale)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dpiScale, 0.0);
        return ClampScale(scale) / dpiScale;
    }

    /// <summary>Physical pixels occupied by an art-pixel area at a given scale.</summary>
    public static PixelSize ToPhysical(int artWidth, int artHeight, int scale)
    {
        var s = ClampScale(scale);
        return new PixelSize(artWidth * s, artHeight * s);
    }

    /// <summary>
    /// The size to give a WPF window, in device-independent units, so that it
    /// ends up exactly <c>artWidth * scale</c> physical pixels wide. The result
    /// is deliberately allowed to be fractional — it is the physical size that
    /// must land on whole pixels, not the DIP size.
    /// </summary>
    public static DipSize ToDeviceIndependent(int artWidth, int artHeight, int scale, double dpiScale)
    {
        var physical = ToPhysical(artWidth, artHeight, scale);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dpiScale, 0.0);
        return new DipSize(physical.Width / dpiScale, physical.Height / dpiScale);
    }

    /// <summary>
    /// Largest integer scale at which the window still fits inside the given
    /// physical work area. Used to pick a sensible default on first run rather
    /// than opening a 4x window on a laptop panel.
    /// </summary>
    public static int LargestScaleThatFits(int artWidth, int artHeight, int workAreaWidth, int workAreaHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(artWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(artHeight);

        for (var scale = MaxScale; scale > MinScale; scale--)
        {
            if (artWidth * scale <= workAreaWidth && artHeight * scale <= workAreaHeight)
            {
                return scale;
            }
        }

        return MinScale;
    }

    /// <summary>
    /// Snaps a freely-dragged size down to the window's resize quantum, so the
    /// library window can only ever be a whole number of sprite tiles.
    /// </summary>
    public static int QuantiseLength(double requested, int minimum, int step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);

        if (requested <= minimum)
        {
            return minimum;
        }

        var steps = (int)Math.Round((requested - minimum) / step, MidpointRounding.AwayFromZero);
        return minimum + (steps * step);
    }
}
