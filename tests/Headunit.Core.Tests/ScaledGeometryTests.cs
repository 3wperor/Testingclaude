using Headunit.Core.Layout;
using Xunit;

namespace Headunit.Core.Tests;

public class ScaledGeometryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(9, 4)]
    public void ClampScale_keeps_scale_within_supported_range(int requested, int expected)
        => Assert.Equal(expected, ScaledGeometry.ClampScale(requested));

    /// <summary>
    /// The load-bearing property of the whole scaling design: whatever the
    /// monitor's DPI scale, the window must come out an exact whole number of
    /// physical pixels. If this ever fails, the art is smeared.
    /// </summary>
    [Theory]
    [InlineData(1.0)]   // 96 dpi
    [InlineData(1.25)]  // 120 dpi
    [InlineData(1.5)]   // 144 dpi
    [InlineData(1.75)]
    [InlineData(2.0)]   // 192 dpi
    public void Physical_size_is_exact_whole_pixels_at_every_dpi_and_scale(double dpiScale)
    {
        for (var scale = ScaledGeometry.MinScale; scale <= ScaledGeometry.MaxScale; scale++)
        {
            var dips = ScaledGeometry.ToDeviceIndependent(
                WindowMetrics.MainWidth, WindowMetrics.MainHeight, scale, dpiScale);

            var physicalWidth = dips.Width * dpiScale;
            var physicalHeight = dips.Height * dpiScale;

            Assert.Equal(WindowMetrics.MainWidth * scale, physicalWidth, precision: 9);
            Assert.Equal(WindowMetrics.MainHeight * scale, physicalHeight, precision: 9);
        }
    }

    [Theory]
    [InlineData(1, 1.0, 1.0)]
    [InlineData(2, 1.0, 2.0)]
    [InlineData(1, 2.0, 0.5)]
    [InlineData(3, 1.5, 2.0)]
    public void LayoutScale_cancels_out_the_dpi_scale(int scale, double dpiScale, double expected)
        => Assert.Equal(expected, ScaledGeometry.LayoutScale(scale, dpiScale), precision: 9);

    [Fact]
    public void LayoutScale_rejects_a_nonsensical_dpi_scale()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ScaledGeometry.LayoutScale(1, 0));

    [Fact]
    public void ToPhysical_multiplies_by_the_clamped_scale()
        => Assert.Equal(new PixelSize(550, 232), ScaledGeometry.ToPhysical(275, 116, 2));

    [Theory]
    [InlineData(3840, 2160, 4)]
    [InlineData(1920, 1080, 4)]
    [InlineData(1366, 768, 4)]
    [InlineData(800, 400, 2)]   // 275*3 = 825 is too wide
    [InlineData(300, 200, 1)]   // only 1x fits
    [InlineData(100, 50, 1)]    // nothing fits; never return 0
    public void LargestScaleThatFits_picks_the_biggest_scale_that_still_fits(int w, int h, int expected)
        => Assert.Equal(expected, ScaledGeometry.LargestScaleThatFits(
            WindowMetrics.MainWidth, WindowMetrics.MainHeight, w, h));

    [Theory]
    [InlineData(275, 275)]   // at the minimum
    [InlineData(100, 275)]   // below the minimum
    [InlineData(287, 275)]   // rounds down to the minimum
    [InlineData(290, 300)]   // rounds up one step
    [InlineData(300, 300)]   // exactly one step
    [InlineData(349, 350)]
    public void QuantiseLength_snaps_to_whole_sprite_tiles(double requested, int expected)
        => Assert.Equal(expected, ScaledGeometry.QuantiseLength(
            requested, WindowMetrics.LibraryMinWidth, WindowMetrics.LibraryWidthStep));

    [Fact]
    public void QuantiseLength_rejects_a_zero_step()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ScaledGeometry.QuantiseLength(400, 275, 0));
}
