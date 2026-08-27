using Headunit.Core.Playback;
using Xunit;

namespace Headunit.Core.Tests;

public class PlayerControllerTests
{
    private static (PlayerController Player, FakeAudioEngine Engine) Build(int trackCount = 3)
    {
        var engine = new FakeAudioEngine();
        var player = new PlayerController(engine);
        player.Queue.Replace(
            Enumerable.Range(0, trackCount).Select(i => new Track($"/music/{i}.mp3", $"Track {i}")));
        return (player, engine);
    }

    [Fact]
    public void PlayAt_loads_and_starts_the_requested_track()
    {
        var (player, engine) = Build();
        player.PlayAt(1);

        Assert.Equal("/music/1.mp3", engine.LoadedPath);
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void TogglePlayPause_cycles_play_pause_and_resumes()
    {
        var (player, _) = Build();
        player.PlayAt(0);

        player.TogglePlayPause();
        Assert.Equal(PlaybackState.Paused, player.State);

        player.TogglePlayPause();
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void TogglePlayPause_from_stopped_starts_the_cued_track()
    {
        var (player, engine) = Build();
        player.TogglePlayPause();

        Assert.Equal(PlaybackState.Playing, player.State);
        Assert.Equal("/music/0.mp3", engine.LoadedPath);
    }

    [Fact]
    public void A_track_ending_advances_the_queue_by_itself()
    {
        var (player, engine) = Build();
        player.PlayAt(0);

        engine.RaiseEnded();

        Assert.Equal("/music/1.mp3", engine.LoadedPath);
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void The_end_of_the_queue_stops_rather_than_wrapping()
    {
        var (player, engine) = Build(2);
        player.PlayAt(1);

        engine.RaiseEnded();

        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void Stop_does_not_advance_the_queue()
    {
        var (player, engine) = Build();
        player.PlayAt(0);

        player.Stop();

        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.Single(engine.LoadHistory);
        Assert.Equal(0, player.Queue.CurrentIndex);
    }

    [Fact]
    public void Previous_restarts_the_track_once_past_the_threshold()
    {
        var (player, engine) = Build();
        player.PlayAt(1);
        engine.Position = TimeSpan.FromSeconds(10);

        player.Previous();

        Assert.Equal("/music/1.mp3", engine.LoadedPath);
        Assert.Equal(TimeSpan.Zero, engine.Position);
    }

    [Fact]
    public void Previous_goes_back_a_track_when_near_the_start()
    {
        var (player, engine) = Build();
        player.PlayAt(1);
        engine.Position = TimeSpan.FromSeconds(1);

        player.Previous();

        Assert.Equal("/music/0.mp3", engine.LoadedPath);
    }

    [Fact]
    public void Previous_on_the_first_track_restarts_it()
    {
        var (player, engine) = Build();
        player.PlayAt(0);
        engine.Position = TimeSpan.FromSeconds(1);

        player.Previous();

        Assert.Equal(TimeSpan.Zero, engine.Position);
        Assert.Equal(0, player.Queue.CurrentIndex);
    }

    [Fact]
    public void A_failing_track_is_reported_and_skipped_rather_than_wedging_the_queue()
    {
        var (player, engine) = Build();
        PlaybackFailedEventArgs? reported = null;
        player.TrackFailed += (_, e) => reported = e;

        player.PlayAt(0);
        engine.RaiseFailed("/music/0.mp3");

        Assert.NotNull(reported);
        Assert.Equal("/music/0.mp3", reported!.Path);
        Assert.Equal("/music/1.mp3", engine.LoadedPath);
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void Seek_is_clamped_to_the_track()
    {
        var (player, engine) = Build();
        player.PlayAt(0);
        engine.Duration = TimeSpan.FromSeconds(200);

        player.Seek(TimeSpan.FromSeconds(-5));
        Assert.Equal(TimeSpan.Zero, engine.Position);

        player.Seek(TimeSpan.FromSeconds(9999));
        Assert.Equal(TimeSpan.FromSeconds(200), engine.Position);
    }

    [Fact]
    public void Volume_is_clamped_to_unity()
    {
        var (player, _) = Build();

        player.Volume = 3f;
        Assert.Equal(1f, player.Volume);

        player.Volume = -1f;
        Assert.Equal(0f, player.Volume);
    }

    [Fact]
    public void TrackChanged_fires_once_per_new_track()
    {
        var (player, engine) = Build();
        var changes = 0;
        player.TrackChanged += (_, _) => changes++;

        player.PlayAt(0);
        engine.RaiseEnded();

        Assert.Equal(2, changes);
    }
}
