using Headunit.Core.Graphics;
using Headunit.Core.Playback;
using Headunit.Core.Themes;

namespace Headunit.Core.Ui;

/// <summary>
/// Draws one frame of the main window. Pure: the same view state, input state,
/// theme and clock always produce the same pixels, which is what lets the
/// preview tool and the tests see exactly what a user sees.
/// </summary>
public sealed class MainRenderer
{
    /// <summary>A paused clock flashes, on for half a second and off for half.</summary>
    public static readonly TimeSpan BlinkPeriod = TimeSpan.FromSeconds(1);

    private const string IdleText = "DROP FILES OR A FOLDER HERE";

    private string _rawText = string.Empty;
    private string _foldedText = string.Empty;

    public MainRenderer(MainArt art, Theme theme)
    {
        Art = art;
        Tinted = TintedAtlas.Bake(art.Atlas, theme);
    }

    public MainArt Art { get; }

    public TintedAtlas Tinted { get; private set; }

    public Theme Theme => Tinted.Theme;

    /// <summary>Re-bakes the atlas. Costs one pass over the atlas; never done per frame.</summary>
    public void SetTheme(Theme theme) => Tinted = TintedAtlas.Bake(Art.Atlas, theme);

    public void Render(Surface frame, MainViewState view, MainInput input, TimeSpan now)
    {
        if (frame.Width != MainLayout.Window.Width || frame.Height != MainLayout.Window.Height)
        {
            throw new ArgumentException("Frame must be the size of the main window.", nameof(frame));
        }

        var t = Tinted;
        frame.Blit(t, Art.Background, 0, 0);

        frame.Blit(t, view.IsActive ? Art.TitleBarActive : Art.TitleBarInactive, 0, 0);
        DrawButton(frame, Art.Minimize, MainLayout.Minimize, input.IsPressed(MainWidget.Minimize));
        DrawButton(frame, Art.Close, MainLayout.Close, input.IsPressed(MainWidget.Close));

        DrawClock(frame, view, now);
        DrawIndicators(frame, view);
        DrawMarquee(frame, view, input, now);
        DrawStreamInfo(frame, view);
        DrawSliders(frame, view, input);

        DrawButton(frame, Art.Previous, MainLayout.Previous, input.IsPressed(MainWidget.Previous));
        DrawButton(frame, Art.Play, MainLayout.Play, input.IsPressed(MainWidget.Play));
        DrawButton(frame, Art.Pause, MainLayout.Pause, input.IsPressed(MainWidget.Pause));
        DrawButton(frame, Art.Stop, MainLayout.Stop, input.IsPressed(MainWidget.Stop));
        DrawButton(frame, Art.Next, MainLayout.Next, input.IsPressed(MainWidget.Next));
        DrawButton(frame, Art.Eject, MainLayout.Eject, input.IsPressed(MainWidget.Eject));
        DrawButton(frame, Art.Equalizer, MainLayout.EqToggle, input.IsPressed(MainWidget.EqToggle));
        DrawButton(frame, Art.Playlist, MainLayout.PlaylistToggle, input.IsPressed(MainWidget.PlaylistToggle));
        DrawButton(frame, view.Shuffle ? Art.ShuffleOn : Art.ShuffleOff, MainLayout.Shuffle, input.IsPressed(MainWidget.Shuffle));
        DrawButton(frame, Art.Repeat[view.Repeat], MainLayout.Repeat, input.IsPressed(MainWidget.Repeat));
    }

    private void DrawButton(Surface frame, ButtonSprites sprites, PixelRect at, bool pressed)
        => frame.Blit(Tinted, sprites.For(pressed), at.X, at.Y);

    private void DrawClock(Surface frame, MainViewState view, TimeSpan now)
    {
        if (string.IsNullOrEmpty(view.Title))
        {
            return; // nothing cued: ghosts only
        }

        var status = view.State switch
        {
            PlaybackState.Playing => Art.StatusPlay,
            PlaybackState.Paused => Art.StatusPause,
            _ => Art.StatusStop,
        };
        var s = MainLayout.Status;
        frame.Blit(Tinted, status, s.X + ((s.Width - status.Core.Width) / 2), s.Y + ((s.Height - status.Core.Height) / 2));

        var blinkedOff = view.State == PlaybackState.Paused
            && (now.Ticks / (BlinkPeriod.Ticks / 2)) % 2 == 1;
        if (blinkedOff)
        {
            return;
        }

        var clock = LcdClock.From(view.Position, view.Duration, view.ShowRemaining);
        if (clock.Negative)
        {
            frame.Blit(Tinted, Art.Minus, MainLayout.Sign.X, MainLayout.Sign.Y);
        }

        int?[] digits = [clock.D0, clock.D1, clock.D2, clock.D3];
        for (var i = 0; i < digits.Length; i++)
        {
            if (digits[i] is { } d)
            {
                frame.Blit(Tinted, Art.Digits[d], MainLayout.Digits[i].X, MainLayout.Digits[i].Y);
            }
        }

        frame.Blit(Tinted, Art.Colon, MainLayout.Colon.X, MainLayout.Colon.Y);
    }

    private void DrawIndicators(Surface frame, MainViewState view)
    {
        if (view.Shuffle)
        {
            Blit(frame, Art.RandomIndicator, MainLayout.RandomIndicator);
        }

        if (view.Repeat != RepeatMode.Off)
        {
            Blit(frame, Art.RepeatIndicator, MainLayout.RepeatIndicator);
        }

        if (view.Repeat == RepeatMode.One)
        {
            Blit(frame, Art.RepeatOneIndicator, MainLayout.RepeatOneIndicator);
        }
    }

    private void DrawMarquee(Surface frame, MainViewState view, MainInput input, TimeSpan now)
    {
        // Precedence: what your hand is doing, then what just happened, then the track.
        string text;
        var scrolls = false;
        if (input.IsDragging(MainWidget.Volume))
        {
            text = TitleText.Volume((float)input.DragValue);
        }
        else if (input.IsDragging(MainWidget.Balance))
        {
            text = TitleText.Balance((float)input.DragValue);
        }
        else if (input.IsDragging(MainWidget.Seek))
        {
            text = TitleText.SeekTo(input.DragValue, view.Duration);
        }
        else if (view.MessageAt(now) is { } message)
        {
            text = message;
        }
        else
        {
            text = string.IsNullOrEmpty(view.Title) ? IdleText : view.Title;
            scrolls = true;
        }

        var folded = Fold(text);
        var font = Art.DotFont.Font;
        var field = MainLayout.TitleField;
        var width = font.Measure(folded);

        if (!scrolls || width <= field.Width)
        {
            DrawText(frame, folded, field.X, field.Y, field);
            return;
        }

        var period = width + font.Measure(Marquee.Separator) + 2;
        var offset = Marquee.Offset(width, field.Width, period, now - view.TitleSince);
        var x = field.X - offset;
        DrawText(frame, folded + Marquee.Separator + folded, x, field.Y, field);
    }

    private void DrawStreamInfo(Surface frame, MainViewState view)
    {
        if (view.Info is not { } info || string.IsNullOrEmpty(view.Title))
        {
            return;
        }

        var labelsClip = MainLayout.RightLcd.Inflate(-2);
        if (info.Kbps is { } kbps)
        {
            DrawRightAligned(frame, kbps.ToString(System.Globalization.CultureInfo.InvariantCulture), MainLayout.Kbps, 4, labelsClip);
            Blit(frame, Art.KbpsLabel, MainLayout.KbpsLabel);
        }

        DrawRightAligned(frame, info.Khz.ToString(System.Globalization.CultureInfo.InvariantCulture), MainLayout.Khz, 3, labelsClip);
        Blit(frame, Art.KhzLabel, MainLayout.KhzLabel);
        Blit(frame, info.IsStereo ? Art.StereoLabel : Art.MonoLabel, info.IsStereo ? MainLayout.StereoLabel : MainLayout.MonoLabel);
    }

    private void DrawSliders(Surface frame, MainViewState view, MainInput input)
    {
        var lit = Tinted.Color(Token.ButtonActive, 200);
        var litDim = Tinted.Color(Token.ButtonActive, 120);

        // Volume: the groove fills up to the thumb, like a level meter.
        var volume = input.IsDragging(MainWidget.Volume) ? input.DragValue : view.Volume;
        var vs = MainLayout.VolumeSlider;
        var vx = vs.ThumbX(volume);
        FillGroove(frame, new PixelRect(vs.Track.X, vs.Track.Y + 4, vs.Track.Width, 5), vx + (vs.ThumbWidth / 2), lit);
        frame.Blit(Tinted, Art.SmallThumb.For(input.IsDragging(MainWidget.Volume)), vx, vs.Track.Y + 1);

        var balance = input.IsDragging(MainWidget.Balance) ? input.DragValue : view.Balance;
        var bs = MainLayout.BalanceSlider;
        frame.Blit(Tinted, Art.SmallThumb.For(input.IsDragging(MainWidget.Balance)), bs.ThumbX((balance + 1) / 2), bs.Track.Y + 1);

        // Seek: no thumb at all until something seekable is loaded.
        if (!view.CanSeek)
        {
            return;
        }

        var dragging = input.IsDragging(MainWidget.Seek);
        var ss = MainLayout.SeekSlider;
        var played = ss.ThumbX(view.SeekFraction) + (ss.ThumbWidth / 2);
        FillGroove(frame, new PixelRect(ss.Track.X, ss.Track.Y + 2, ss.Track.Width, 6), played, litDim);
        frame.Blit(Tinted, Art.SeekThumb.For(dragging), ss.ThumbX(dragging ? input.DragValue : view.SeekFraction), ss.Track.Y);
    }

    /// <summary>Lights the inside of a groove from its left end up to <paramref name="untilX"/>.</summary>
    private static void FillGroove(Surface frame, PixelRect groove, int untilX, uint color)
    {
        var inner = groove.Inflate(-1);
        var width = Math.Clamp(untilX - inner.X, 0, inner.Width);
        frame.FillRect(inner with { Width = width }, color);
    }

    private void DrawRightAligned(Surface frame, string text, (int X, int Y) at, int cells, PixelRect clip)
    {
        var font = Art.DotFont.Font;
        var x = at.X + ((cells - text.Length) * font.Advance);
        DrawText(frame, text, x, at.Y, clip);
    }

    /// <summary>Draws pre-folded text: every halo first, then every core.</summary>
    private void DrawText(Surface frame, string folded, int x, int y, PixelRect clip)
    {
        var font = Art.DotFont;
        var advance = font.Font.Advance;

        // Skip glyphs wholly left of the clip; stop once past its right edge.
        var first = Math.Max(0, (clip.X - x - advance) / advance);
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = first; i < folded.Length; i++)
            {
                var gx = x + (i * advance);
                if (gx > clip.Right)
                {
                    break;
                }

                if (!font.Lit.TryGetValue(folded[i], out var glyph))
                {
                    continue;
                }

                frame.Blit(Tinted, pass == 0 ? glyph.Halo : glyph.Core, pass == 0 ? gx - 1 : gx, pass == 0 ? y - 1 : y, clip);
            }
        }
    }

    private void Blit(Surface frame, LitSprite sprite, (int X, int Y) at) => frame.Blit(Tinted, sprite, at.X, at.Y);

    private string Fold(string text)
    {
        if (!string.Equals(text, _rawText, StringComparison.Ordinal))
        {
            _rawText = text;
            _foldedText = Art.DotFont.Font.Fold(text);
        }

        return _foldedText;
    }
}
