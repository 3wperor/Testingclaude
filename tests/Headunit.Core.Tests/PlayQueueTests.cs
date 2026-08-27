using Headunit.Core.Playback;
using Xunit;

namespace Headunit.Core.Tests;

public class PlayQueueTests
{
    private static PlayQueue QueueOf(int count, Random? random = null)
    {
        var queue = new PlayQueue(random);
        queue.Replace(Enumerable.Range(0, count).Select(i => new Track($"/music/{i}.mp3", $"Track {i}")));
        return queue;
    }

    [Fact]
    public void An_empty_queue_has_no_current_track()
    {
        var queue = new PlayQueue();
        Assert.Equal(-1, queue.CurrentIndex);
        Assert.Null(queue.Current);
        Assert.False(queue.MoveNext(userInitiated: true));
        Assert.False(queue.MovePrevious());
    }

    [Fact]
    public void Replace_cues_the_first_track()
    {
        var queue = QueueOf(3);
        Assert.Equal(0, queue.CurrentIndex);
        Assert.Equal("Track 0", queue.Current!.Title);
    }

    [Fact]
    public void MoveNext_stops_at_the_end_when_repeat_is_off()
    {
        var queue = QueueOf(2);
        Assert.True(queue.MoveNext(userInitiated: true));
        Assert.False(queue.MoveNext(userInitiated: true));
        Assert.Equal(1, queue.CurrentIndex); // cursor stays put
    }

    [Fact]
    public void MoveNext_wraps_when_repeat_is_all()
    {
        var queue = QueueOf(2);
        queue.Repeat = RepeatMode.All;
        queue.MoveNext(userInitiated: true);
        Assert.True(queue.MoveNext(userInitiated: true));
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void Repeat_one_holds_position_when_a_track_ends_by_itself()
    {
        var queue = QueueOf(3);
        queue.Repeat = RepeatMode.One;
        Assert.True(queue.MoveNext(userInitiated: false));
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void Repeat_one_still_lets_the_user_skip_forward()
    {
        var queue = QueueOf(3);
        queue.Repeat = RepeatMode.One;
        Assert.True(queue.MoveNext(userInitiated: true));
        Assert.Equal(1, queue.CurrentIndex);
    }

    [Fact]
    public void MovePrevious_stops_at_the_start_unless_repeating_all()
    {
        var queue = QueueOf(3);
        Assert.False(queue.MovePrevious());

        queue.Repeat = RepeatMode.All;
        Assert.True(queue.MovePrevious());
        Assert.Equal(2, queue.CurrentIndex);
    }

    /// <summary>
    /// The property that separates shuffle from "random": one pass must visit
    /// every track exactly once.
    /// </summary>
    [Fact]
    public void Shuffle_visits_every_track_exactly_once_per_pass()
    {
        var queue = QueueOf(25, new Random(1234));
        queue.Shuffle = true;

        var visited = new List<int> { queue.CurrentIndex };
        while (queue.MoveNext(userInitiated: true))
        {
            visited.Add(queue.CurrentIndex);
        }

        Assert.Equal(25, visited.Count);
        Assert.Equal(Enumerable.Range(0, 25), visited.OrderBy(i => i));
    }

    [Fact]
    public void Shuffle_actually_reorders()
    {
        var queue = QueueOf(50, new Random(7));
        queue.Shuffle = true;

        var visited = new List<int> { queue.CurrentIndex };
        while (queue.MoveNext(userInitiated: true))
        {
            visited.Add(queue.CurrentIndex);
        }

        Assert.NotEqual(Enumerable.Range(0, 50), visited);
    }

    [Fact]
    public void Turning_shuffle_on_does_not_interrupt_the_playing_track()
    {
        var queue = QueueOf(10, new Random(99));
        queue.MoveTo(6);

        queue.Shuffle = true;

        Assert.Equal(6, queue.CurrentIndex);
    }

    [Fact]
    public void Turning_shuffle_off_restores_natural_order_from_the_current_track()
    {
        var queue = QueueOf(10, new Random(99));
        queue.Shuffle = true;
        queue.MoveTo(4);

        queue.Shuffle = false;

        Assert.Equal(4, queue.CurrentIndex);
        Assert.True(queue.MoveNext(userInitiated: true));
        Assert.Equal(5, queue.CurrentIndex);
    }

    [Fact]
    public void Append_keeps_the_current_track_cued()
    {
        var queue = QueueOf(3);
        queue.MoveTo(2);

        queue.Append([new Track("/music/x.mp3", "X")]);

        Assert.Equal(2, queue.CurrentIndex);
        Assert.Equal(4, queue.Count);
        Assert.True(queue.MoveNext(userInitiated: true));
        Assert.Equal("X", queue.Current!.Title);
    }

    [Fact]
    public void MoveTo_rejects_an_out_of_range_index()
    {
        var queue = QueueOf(3);
        Assert.False(queue.MoveTo(7));
        Assert.False(queue.MoveTo(-1));
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void Clear_empties_the_queue()
    {
        var queue = QueueOf(3);
        queue.Clear();
        Assert.Equal(0, queue.Count);
        Assert.Null(queue.Current);
    }
}
