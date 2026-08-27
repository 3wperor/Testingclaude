using Headunit.Core.Playback;
using Xunit;

namespace Headunit.Core.Tests;

public class TimeDisplayTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(3, "0:03")]
    [InlineData(65, "1:05")]
    [InlineData(599, "9:59")]
    [InlineData(600, "10:00")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3725, "1:02:05")]
    public void Elapsed_time_reads_like_a_transport(int seconds, string expected)
        => Assert.Equal(expected, TimeDisplay.Format(
            TimeSpan.FromSeconds(seconds), TimeSpan.FromHours(2), showRemaining: false));

    [Fact]
    public void Remaining_time_carries_a_leading_minus()
        => Assert.Equal("-2:35", TimeDisplay.Format(
            TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(180), showRemaining: true));

    [Fact]
    public void Remaining_time_never_goes_negative_past_the_end()
        => Assert.Equal("-0:00", TimeDisplay.Format(
            TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(180), showRemaining: true));

    [Fact]
    public void An_unknown_duration_still_shows_elapsed_time()
        => Assert.Equal("0:42", TimeDisplay.Format(
            TimeSpan.FromSeconds(42), TimeSpan.Zero, showRemaining: false));
}
