using Headunit.Core.Ui;
using Xunit;

namespace Headunit.Core.Tests;

public class MainLayoutTests
{
    [Fact]
    public void Every_target_lies_inside_the_window()
        => Assert.All(MainLayout.HitTargets, t => Assert.True(MainLayout.Window.Contains(t.Rect), t.Widget.ToString()));

    [Fact]
    public void No_two_targets_overlap_except_title_buttons_on_the_title_bar()
    {
        var targets = MainLayout.HitTargets;
        for (var i = 0; i < targets.Count; i++)
        {
            for (var j = i + 1; j < targets.Count; j++)
            {
                var (a, b) = (targets[i], targets[j]);
                var allowed = b.Widget == MainWidget.TitleBar && a.Widget is MainWidget.Minimize or MainWidget.Close;
                if (!allowed)
                {
                    Assert.False(a.Rect.IntersectsWith(b.Rect), $"{a.Widget} overlaps {b.Widget}");
                }
            }
        }
    }

    [Fact]
    public void Every_target_is_reachable_at_its_centre()
    {
        foreach (var (widget, rect) in MainLayout.HitTargets)
        {
            Assert.Equal(widget, MainLayout.HitTest(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2)));
        }
    }

    [Fact]
    public void Lcd_contents_stay_inside_their_glass()
    {
        var left = MainLayout.LeftLcd.Inflate(-1);
        Assert.True(left.Contains(MainLayout.Status));
        Assert.True(left.Contains(MainLayout.Sign));
        Assert.All(MainLayout.Digits, d => Assert.True(left.Contains(d)));
        Assert.True(left.Contains(MainLayout.Visualizer));

        Assert.True(MainLayout.RightLcd.Inflate(-1).Contains(MainLayout.TitleField));
    }

    [Fact]
    public void Clock_cells_never_touch()
    {
        var cells = MainLayout.Digits.Prepend(MainLayout.Sign).Append(MainLayout.Colon).OrderBy(r => r.X).ToList();
        for (var i = 1; i < cells.Count; i++)
        {
            Assert.True(cells[i].X - cells[i - 1].Right >= 2, $"cells at {cells[i - 1].X} and {cells[i].X} are too close");
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.37)]
    [InlineData(1.0)]
    public void Slider_fraction_survives_a_round_trip_through_the_thumb(double fraction)
    {
        var s = MainLayout.SeekSlider;
        var x = s.ThumbX(fraction);
        Assert.Equal(fraction, s.FractionAt(x, 0), 2);
    }

    [Fact]
    public void Slider_thumb_stays_inside_the_track()
    {
        var s = MainLayout.VolumeSlider;
        Assert.Equal(s.Track.X, s.ThumbX(-5));
        Assert.Equal(s.Track.Right - s.ThumbWidth, s.ThumbX(5));
    }
}
