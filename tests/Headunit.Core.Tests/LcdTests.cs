using Headunit.Core.Playback;
using Headunit.Core.Ui;
using Xunit;

namespace Headunit.Core.Tests;

public class LcdClockTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    [Fact]
    public void Leading_minute_zero_is_unlit()
    {
        var c = LcdClock.From(S(65), S(300), showRemaining: false);
        Assert.Equal(new LcdClock(false, null, 1, 0, 5, false), c);
    }

    [Fact]
    public void Two_digit_minutes_light_both_cells()
    {
        var c = LcdClock.From(S((42 * 60) + 7), S(3600), showRemaining: false);
        Assert.Equal((4, 2, 0, 7), (c.D0, c.D1, c.D2, c.D3));
    }

    [Fact]
    public void Remaining_is_negative_and_counts_down()
    {
        var c = LcdClock.From(S(60), S(245), showRemaining: true);
        Assert.True(c.Negative);
        Assert.Equal((null, 3, 0, 5), (c.D0, c.D1, c.D2, c.D3));
    }

    [Fact]
    public void Remaining_never_goes_below_zero()
    {
        var c = LcdClock.From(S(300), S(200), showRemaining: true);
        Assert.Equal((null, 0, 0, 0), (c.D0, c.D1, c.D2, c.D3));
    }

    [Fact]
    public void Past_99_minutes_switches_to_hours_and_minutes()
    {
        var c = LcdClock.From(TimeSpan.FromMinutes(125) + S(30), TimeSpan.FromHours(3), showRemaining: false);
        Assert.True(c.Hours);
        Assert.Equal((null, 2, 0, 5), (c.D0, c.D1, c.D2, c.D3));
    }
}

public class MarqueeTests
{
    [Fact]
    public void Text_that_fits_never_moves()
        => Assert.Equal(0, Marquee.Offset(100, 152, 140, TimeSpan.FromMinutes(5)));

    [Fact]
    public void Long_text_holds_before_scrolling()
    {
        Assert.Equal(0, Marquee.Offset(300, 152, 340, Marquee.Hold));
        Assert.Equal(5, Marquee.Offset(300, 152, 340, Marquee.Hold + (Marquee.Step * 5)));
    }

    [Fact]
    public void Scrolling_wraps_after_one_period()
    {
        const int period = 340;
        var oneLap = Marquee.Hold + (Marquee.Step * period);
        Assert.Equal(0, Marquee.Offset(300, 152, period, oneLap));
        Assert.Equal(1, Marquee.Offset(300, 152, period, oneLap + Marquee.Step));
    }
}

public class TitleTextTests
{
    [Fact]
    public void Composes_the_classic_marquee_line()
    {
        var track = new Track(@"C:\m\a.mp3", "Autobahn");
        Assert.Equal("3. Autobahn (22:43)", TitleText.Compose(2, track, TimeSpan.FromSeconds(1363)));
        Assert.Equal("1. Autobahn", TitleText.Compose(0, track, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(0f, "BALANCE: CENTER")]
    [InlineData(-0.4f, "BALANCE: 40% LEFT")]
    [InlineData(1f, "BALANCE: 100% RIGHT")]
    public void Balance_reads_as_a_side_not_a_sign(float balance, string expected)
        => Assert.Equal(expected, TitleText.Balance(balance));

    [Fact]
    public void Seek_preview_shows_target_total_and_percent()
        => Assert.Equal("SEEK TO: 1:00/4:00 (25%)", TitleText.SeekTo(0.25, TimeSpan.FromMinutes(4)));

    [Fact]
    public void Volume_is_a_whole_percentage()
        => Assert.Equal("VOLUME: 72%", TitleText.Volume(0.7249f));
}

public class StreamInfoTests
{
    [Fact]
    public void Kbps_is_estimated_from_size_and_duration()
    {
        // 4 minutes at a true 320 kbps.
        var bytes = 320_000L / 8 * 240;
        Assert.Equal(320, StreamInfo.EstimateKbps(bytes, TimeSpan.FromMinutes(4)));
    }

    [Fact]
    public void Kbps_is_unknown_without_a_duration()
        => Assert.Null(StreamInfo.EstimateKbps(1000, TimeSpan.Zero));

    [Theory]
    [InlineData(44100, 44)]
    [InlineData(48000, 48)]
    [InlineData(22050, 22)]
    [InlineData(192000, 192)]
    public void Khz_truncates_like_the_hardware(int rate, int khz)
        => Assert.Equal(khz, new StreamInfo(rate, 2, null).Khz);
}

public class BalanceLawTests
{
    [Theory]
    [InlineData(0f, 1f, 1f)]
    [InlineData(-1f, 1f, 0f)]
    [InlineData(1f, 0f, 1f)]
    [InlineData(0.25f, 0.75f, 1f)]
    [InlineData(-0.5f, 1f, 0.5f)]
    [InlineData(7f, 0f, 1f)]
    public void Only_ever_attenuates_the_far_channel(float balance, float left, float right)
    {
        var (l, r) = BalanceLaw.Gains(balance);
        Assert.Equal(left, l, 5);
        Assert.Equal(right, r, 5);
    }
}
