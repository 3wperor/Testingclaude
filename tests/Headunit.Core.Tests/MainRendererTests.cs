using Headunit.Core.Graphics;
using Headunit.Core.Layout;
using Headunit.Core.Playback;
using Headunit.Core.Themes;
using Headunit.Core.Ui;
using Xunit;

namespace Headunit.Core.Tests;

public class MainRendererTests
{
    // Building the art is the expensive part; share it across tests.
    private static readonly MainArt Art = MainArt.Build();

    private readonly MainRenderer _renderer = new(Art, ThemePresets.Default);
    private readonly MainViewState _view = Playing();

    private static MainViewState Playing()
    {
        var view = new MainViewState
        {
            State = PlaybackState.Playing,
            Position = TimeSpan.FromSeconds(83),
            Duration = TimeSpan.FromSeconds(332),
            Volume = 0.7f,
            Info = new StreamInfo(44100, 2, 192),
        };
        view.SetTitle("3. A title long enough that it has to scroll across the field (5:32)", TimeSpan.Zero);
        return view;
    }

    private Surface Render(MainViewState view, MainInput? input = null, TimeSpan now = default)
    {
        var frame = new Surface(WindowMetrics.MainWidth, WindowMetrics.MainHeight);
        _renderer.Render(frame, view, input ?? new MainInput(view), now);
        return frame;
    }

    private static List<(int X, int Y)> Diff(Surface a, Surface b)
    {
        var changed = new List<(int, int)>();
        for (var y = 0; y < a.Height; y++)
        {
            for (var x = 0; x < a.Width; x++)
            {
                if (a[x, y] != b[x, y])
                {
                    changed.Add((x, y));
                }
            }
        }

        return changed;
    }

    public static TheoryData<string> PresetNames => new(ThemePresets.All.Select(t => t.Name));

    [Theory]
    [MemberData(nameof(PresetNames))]
    public void Every_pixel_of_every_preset_is_opaque(string name)
    {
        _renderer.SetTheme(ThemePresets.All.Single(t => t.Name == name));
        var frame = Render(_view);
        Assert.All(frame.Pixels, p => Assert.Equal(255, Argb.A(p)));
    }

    [Fact]
    public void Rendering_is_deterministic()
        => Assert.Equal(Render(_view).Pixels, Render(_view).Pixels);

    [Fact]
    public void Changing_theme_recolours_the_lcd()
    {
        var amber = Render(_view);
        _renderer.SetTheme(ThemePresets.AlpineIce);
        var ice = Render(_view);

        var field = MainLayout.TitleField;
        Assert.Contains(Diff(amber, ice), p => field.Contains(p.X, p.Y));
    }

    [Fact]
    public void Pressing_a_button_changes_only_that_button()
    {
        var input = new MainInput(_view);
        var idle = Render(_view, input);

        var r = MainLayout.Pause;
        input.PointerDown(r.X + 5, r.Y + 5);
        var pressed = Render(_view, input);

        var changed = Diff(idle, pressed);
        Assert.NotEmpty(changed);
        Assert.All(changed, p => Assert.True(r.Contains(p.X, p.Y), $"{p} is outside the button"));
    }

    [Fact]
    public void The_marquee_scrolls_inside_its_field_and_nowhere_else()
    {
        var start = Render(_view, now: TimeSpan.Zero);
        var later = Render(_view, now: Marquee.Hold + TimeSpan.FromSeconds(1));

        var changed = Diff(start, later);
        Assert.NotEmpty(changed);

        // Glyph halos may bleed one pixel past the field edge, never more.
        var allowed = MainLayout.TitleField.Inflate(1);
        Assert.All(changed, p => Assert.True(allowed.Contains(p.X, p.Y), $"{p}"));
    }

    [Fact]
    public void A_paused_clock_blinks()
    {
        _view.State = PlaybackState.Paused;
        var on = Render(_view, now: TimeSpan.Zero);
        var off = Render(_view, now: MainRenderer.BlinkPeriod / 2);

        var clock = new PixelRect(MainLayout.Sign.X - 1, MainLayout.Sign.Y - 1, 60, 15);
        Assert.Contains(Diff(on, off), p => clock.Contains(p.X, p.Y));
    }

    [Fact]
    public void Remaining_time_lights_the_minus_sign()
    {
        var elapsed = Render(_view);
        _view.ShowRemaining = true;
        var remaining = Render(_view);

        var sign = MainLayout.Sign;
        Assert.Contains(Diff(elapsed, remaining), p => sign.Contains(p.X, p.Y));
    }

    [Fact]
    public void No_seek_thumb_until_something_is_loaded()
    {
        var empty = new MainViewState();
        var frame = Render(empty);
        var loaded = Render(_view);

        var seek = MainLayout.Seek;
        Assert.Contains(Diff(frame, loaded), p => seek.Contains(p.X, p.Y));
        Assert.DoesNotContain(Diff(frame, Render(new MainViewState())), _ => true);
    }

    [Fact]
    public void Dragging_volume_replaces_the_title_with_a_readout()
    {
        var input = new MainInput(_view);
        var before = Render(_view, input);

        var s = MainLayout.VolumeSlider;
        input.PointerDown(s.ThumbX(_view.Volume) + 2, s.Track.Y + 5);
        input.PointerMove(s.Track.X, s.Track.Y + 5);
        var during = Render(_view, input);

        var changed = Diff(before, during);
        Assert.Contains(changed, p => MainLayout.TitleField.Contains(p.X, p.Y));
        Assert.Contains(changed, p => MainLayout.Volume.Contains(p.X, p.Y));
    }

    [Fact]
    public void Inactive_window_dims_only_the_title_bar()
    {
        var active = Render(_view);
        _view.IsActive = false;
        var inactive = Render(_view);

        var changed = Diff(active, inactive);
        Assert.NotEmpty(changed);
        Assert.All(changed, p => Assert.True(MainLayout.TitleBar.Contains(p.X, p.Y)));
    }

    [Fact]
    public void Rejects_a_frame_of_the_wrong_size()
        => Assert.Throws<ArgumentException>(
            () => _renderer.Render(new Surface(10, 10), _view, new MainInput(_view), TimeSpan.Zero));
}

public class MainArtTests
{
    private static readonly MainArt Art = MainArt.Build();

    [Fact]
    public void Sprites_never_overlap_in_the_atlas()
    {
        var sprites = Art.Atlas.Sprites.Select(s => s.Sprite).OrderBy(s => s.Source.Y).ThenBy(s => s.Source.X).ToList();
        for (var i = 0; i < sprites.Count; i++)
        {
            for (var j = i + 1; j < sprites.Count && sprites[j].Source.Y < sprites[i].Source.Bottom; j++)
            {
                Assert.False(sprites[i].Source.IntersectsWith(sprites[j].Source));
            }
        }
    }

    [Fact]
    public void Every_dot_font_glyph_is_baked()
        => Assert.All(PixelFont.Dot.Characters, c => Assert.True(Art.DotFont.Lit.ContainsKey(c)));

    [Fact]
    public void Background_covers_the_whole_window_with_opaque_pixels()
    {
        var mask = Art.Atlas.Mask;
        var bg = Art.Background.Source;
        for (var y = bg.Y; y < bg.Bottom; y++)
        {
            for (var x = bg.X; x < bg.Right; x++)
            {
                var (token, _, alpha) = mask.Get(x, y);
                Assert.NotEqual(Token.None, token);
                Assert.Equal(255, alpha);
            }
        }
    }

    [Fact]
    public void Atlas_only_references_real_tokens()
        => Assert.All(Art.Atlas.Mask.Tokens, t => Assert.True(t < Tokens.Count));

    [Fact]
    public void Seven_segment_digits_light_the_right_segments()
    {
        static int Lit(string[] rows) => rows.Sum(r => r.Count(c => c == '#'));

        Assert.Equal(Lit(SevenSegment.Pattern(SevenSegment.All)), Lit(SevenSegment.Pattern(SevenSegment.ForDigit(8))));
        Assert.True(Lit(SevenSegment.Pattern(SevenSegment.ForDigit(1))) < Lit(SevenSegment.Pattern(SevenSegment.ForDigit(7))));
        Assert.Equal(0, Lit(SevenSegment.Pattern(string.Empty)));
    }
}
