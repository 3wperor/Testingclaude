using Headunit.Core.Graphics;
using Headunit.Core.Ui;
using Xunit;

namespace Headunit.Core.Tests;

public class MainInputTests
{
    private readonly MainViewState _view = new() { Volume = 0.5f, Duration = TimeSpan.FromMinutes(4) };
    private readonly MainInput _input;

    public MainInputTests() => _input = new MainInput(_view);

    private static (int X, int Y) Centre(PixelRect r) => (r.X + (r.Width / 2), r.Y + (r.Height / 2));

    [Fact]
    public void A_button_clicks_on_release_over_itself()
    {
        var (x, y) = Centre(MainLayout.Play);

        Assert.Null(_input.PointerDown(x, y));
        Assert.True(_input.IsPressed(MainWidget.Play));

        Assert.Equal(new UiAction(UiActionKind.Click, MainWidget.Play), _input.PointerUp(x, y));
        Assert.False(_input.IsPressed(MainWidget.Play));
    }

    [Fact]
    public void Sliding_off_a_button_before_release_cancels_it()
    {
        var (x, y) = Centre(MainLayout.Stop);
        _input.PointerDown(x, y);

        _input.PointerMove(x, y + 40);
        Assert.False(_input.IsPressed(MainWidget.Stop)); // drawn popped back up

        _input.PointerMove(x, y);
        Assert.True(_input.IsPressed(MainWidget.Stop)); // and down again on return

        Assert.Null(_input.PointerUp(x, y + 40));
    }

    [Fact]
    public void Releasing_over_a_different_button_does_not_click_either()
    {
        var (px, py) = Centre(MainLayout.Previous);
        var (nx, ny) = Centre(MainLayout.Next);
        _input.PointerDown(px, py);
        Assert.Null(_input.PointerUp(nx, ny));
    }

    [Fact]
    public void Title_bar_starts_a_window_drag_but_its_buttons_do_not()
    {
        var (bx, by) = Centre(MainLayout.TitleBar with { Width = 100 });
        Assert.Equal(UiActionKind.DragWindow, _input.PointerDown(bx, by)?.Kind);

        var (cx, cy) = Centre(MainLayout.Close);
        Assert.Null(_input.PointerDown(cx, cy));
        Assert.Equal(new UiAction(UiActionKind.Click, MainWidget.Close), _input.PointerUp(cx, cy));
    }

    [Fact]
    public void Time_readout_toggles_on_press()
    {
        var (x, y) = Centre(MainLayout.TimeReadout);
        Assert.Equal(new UiAction(UiActionKind.Click, MainWidget.TimeReadout), _input.PointerDown(x, y));
    }

    [Fact]
    public void Volume_follows_the_pointer_live_and_clamps()
    {
        var s = MainLayout.VolumeSlider;
        var thumbX = s.ThumbX(_view.Volume);
        var y = s.Track.Y + 5;

        var down = _input.PointerDown(thumbX + 3, y);
        Assert.Equal(UiActionKind.Volume, down?.Kind);
        Assert.Equal(0.5, down!.Value.Value, 2); // grabbing the thumb doesn't jump it

        var moved = _input.PointerMove(thumbX + 3 + 10, y);
        Assert.Equal(0.5 + (10.0 / s.Travel), moved!.Value.Value, 3);

        Assert.Equal(1.0, _input.PointerMove(1000, y)!.Value.Value);
        Assert.Equal(0.0, _input.PointerMove(-1000, y)!.Value.Value);
        Assert.Null(_input.PointerUp(-1000, y)); // volume already applied; release adds nothing
    }

    [Fact]
    public void Clicking_bare_volume_track_centres_the_thumb_under_the_pointer()
    {
        var s = MainLayout.VolumeSlider;
        var target = s.Track.X + 5 + (s.ThumbWidth / 2);
        var action = _input.PointerDown(target, s.Track.Y + 5);
        Assert.Equal(5.0 / s.Travel, action!.Value.Value, 3);
    }

    [Fact]
    public void Balance_snaps_to_centre()
    {
        var s = MainLayout.BalanceSlider;
        var centreX = s.ThumbX(0.5) + (s.ThumbWidth / 2);
        var y = s.Track.Y + 5;

        _input.PointerDown(centreX + 1, y);
        Assert.Equal(0.0, _input.DragValue);

        var far = _input.PointerMove(centreX + 8, y);
        Assert.True(far!.Value.Value > 0.5);
    }

    [Fact]
    public void Seek_previews_while_dragging_and_commits_once_on_release()
    {
        var s = MainLayout.SeekSlider;
        var y = s.Track.Y + 5;

        Assert.Null(_input.PointerDown(s.Track.X + (s.ThumbWidth / 2), y));
        Assert.True(_input.IsDragging(MainWidget.Seek));

        Assert.Null(_input.PointerMove(s.Track.X + (s.ThumbWidth / 2) + (s.Travel / 2), y));
        Assert.Equal(0.5, _input.DragValue, 2);

        var release = _input.PointerUp(s.Track.Right + 50, y);
        Assert.Equal(UiActionKind.Seek, release?.Kind);
        Assert.Equal(1.0, release!.Value.Value);
        Assert.False(_input.IsDragging(MainWidget.Seek));
    }

    [Fact]
    public void Seek_bar_is_inert_with_nothing_loaded()
    {
        var empty = new MainInput(new MainViewState());
        var (x, y) = Centre(MainLayout.Seek);
        Assert.Null(empty.PointerDown(x, y));
        Assert.Equal(MainWidget.None, empty.Captured);
        Assert.Null(empty.PointerUp(x, y));
    }

    [Fact]
    public void Cancel_drops_a_gesture_without_acting()
    {
        var (x, y) = Centre(MainLayout.Next);
        _input.PointerDown(x, y);
        _input.Cancel();
        Assert.Null(_input.PointerUp(x, y));
    }

    [Fact]
    public void Clicking_empty_bezel_does_nothing()
    {
        Assert.Null(_input.PointerDown(3, 110));
        Assert.Equal(MainWidget.None, _input.Captured);
    }
}
