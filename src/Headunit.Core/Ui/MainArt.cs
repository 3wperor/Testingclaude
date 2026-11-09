using Headunit.Core.Graphics;
using Headunit.Core.Playback;
using Headunit.Core.Themes;

namespace Headunit.Core.Ui;

public readonly record struct ButtonSprites(Sprite Normal, Sprite Pressed)
{
    public Sprite For(bool pressed) => pressed ? Pressed : Normal;
}

/// <summary>A font baked into the atlas: lit (with glow) for the LCD, plus a dim variant.</summary>
public sealed class FontSprites(PixelFont font, IReadOnlyDictionary<char, LitSprite> lit)
{
    public PixelFont Font { get; } = font;

    public IReadOnlyDictionary<char, LitSprite> Lit { get; } = lit;
}

/// <summary>
/// The main window's artwork, drawn in code as luminance + token masks.
/// <para>
/// Authoring the art as code rather than as bitmaps keeps it diffable,
/// reviewable and exactly reproducible; <see cref="MaskImage.ToInterchangeImage"/>
/// exports the same pixels as a PNG for anyone who'd rather push pixels by hand.
/// No colour appears anywhere in this file — only luminance and token names.
/// </para>
/// </summary>
public sealed class MainArt
{
    private MainArt()
    {
    }

    public SpriteAtlas Atlas { get; private set; } = null!;

    public Sprite Background { get; private set; }

    public Sprite TitleBarActive { get; private set; }

    public Sprite TitleBarInactive { get; private set; }

    public ButtonSprites Minimize { get; private set; }

    public ButtonSprites Close { get; private set; }

    public ButtonSprites Previous { get; private set; }

    public ButtonSprites Play { get; private set; }

    public ButtonSprites Pause { get; private set; }

    public ButtonSprites Stop { get; private set; }

    public ButtonSprites Next { get; private set; }

    public ButtonSprites Eject { get; private set; }

    public ButtonSprites Equalizer { get; private set; }

    public ButtonSprites Playlist { get; private set; }

    public ButtonSprites ShuffleOff { get; private set; }

    public ButtonSprites ShuffleOn { get; private set; }

    public IReadOnlyDictionary<RepeatMode, ButtonSprites> Repeat { get; private set; } = null!;

    public ButtonSprites SmallThumb { get; private set; }

    public ButtonSprites SeekThumb { get; private set; }

    public IReadOnlyList<LitSprite> Digits { get; private set; } = null!;

    public LitSprite Minus { get; private set; }

    public LitSprite Colon { get; private set; }

    public LitSprite StatusPlay { get; private set; }

    public LitSprite StatusPause { get; private set; }

    public LitSprite StatusStop { get; private set; }

    public LitSprite RandomIndicator { get; private set; }

    public LitSprite RepeatIndicator { get; private set; }

    public LitSprite RepeatOneIndicator { get; private set; }

    public LitSprite KbpsLabel { get; private set; }

    public LitSprite KhzLabel { get; private set; }

    public LitSprite MonoLabel { get; private set; }

    public LitSprite StereoLabel { get; private set; }

    public FontSprites DotFont { get; private set; } = null!;

    public static MainArt Build()
    {
        var b = new SpriteAtlasBuilder(512);
        var art = new MainArt();

        art.Background = b.Add("background", MainLayout.Window.Width, MainLayout.Window.Height, DrawBackground);
        art.TitleBarActive = b.Add("titlebar.active", MainLayout.TitleBar.Width, MainLayout.TitleBar.Height,
            c => DrawTitleBar(c, Token.TitlebarActive, 255));
        art.TitleBarInactive = b.Add("titlebar.inactive", MainLayout.TitleBar.Width, MainLayout.TitleBar.Height,
            c => DrawTitleBar(c, Token.TitlebarInactive, 150));

        art.Minimize = AddButton(b, "minimize", MainLayout.Minimize, Glyphs.Minimize);
        art.Close = AddButton(b, "close", MainLayout.Close, Glyphs.Close);

        art.Previous = AddButton(b, "previous", MainLayout.Previous, Glyphs.Previous);
        art.Play = AddButton(b, "play", MainLayout.Play, Glyphs.Play);
        art.Pause = AddButton(b, "pause", MainLayout.Pause, Glyphs.Pause);
        art.Stop = AddButton(b, "stop", MainLayout.Stop, Glyphs.Stop);
        art.Next = AddButton(b, "next", MainLayout.Next, Glyphs.Next);
        art.Eject = AddButton(b, "eject", MainLayout.Eject, Glyphs.Eject);

        art.Equalizer = AddLabelButton(b, "eq", MainLayout.EqToggle, "EQ", lit: false);
        art.Playlist = AddLabelButton(b, "pl", MainLayout.PlaylistToggle, "PL", lit: false);
        art.ShuffleOff = AddLabelButton(b, "shuffle.off", MainLayout.Shuffle, "SHUFFLE", lit: false);
        art.ShuffleOn = AddLabelButton(b, "shuffle.on", MainLayout.Shuffle, "SHUFFLE", lit: true);
        art.Repeat = new Dictionary<RepeatMode, ButtonSprites>
        {
            [RepeatMode.Off] = AddLabelButton(b, "repeat.off", MainLayout.Repeat, "REPEAT", lit: false),
            [RepeatMode.All] = AddLabelButton(b, "repeat.all", MainLayout.Repeat, "REPEAT", lit: true),
            [RepeatMode.One] = AddLabelButton(b, "repeat.one", MainLayout.Repeat, "RPT 1", lit: true),
        };

        art.SmallThumb = new ButtonSprites(
            b.Add("thumb.small", MainLayout.SmallThumbWidth, MainLayout.SmallThumbHeight, c => DrawThumb(c, false)),
            b.Add("thumb.small.pressed", MainLayout.SmallThumbWidth, MainLayout.SmallThumbHeight, c => DrawThumb(c, true)));
        art.SeekThumb = new ButtonSprites(
            b.Add("thumb.seek", MainLayout.SeekThumbWidth, MainLayout.Seek.Height, c => DrawThumb(c, false)),
            b.Add("thumb.seek.pressed", MainLayout.SeekThumbWidth, MainLayout.Seek.Height, c => DrawThumb(c, true)));

        art.Digits = Enumerable.Range(0, 10)
            .Select(d => b.AddLit($"digit.{d}", SevenSegment.Pattern(SevenSegment.ForDigit(d)), Token.LcdText, Token.LcdGlow))
            .ToArray();
        art.Minus = b.AddLit("digit.minus", SevenSegment.Pattern("g"), Token.LcdText, Token.LcdGlow);
        art.Colon = b.AddLit("digit.colon", Glyphs.Colon, Token.LcdText, Token.LcdGlow);

        art.StatusPlay = b.AddLit("status.play", Glyphs.StatusPlay, Token.LcdText, Token.LcdGlow);
        art.StatusPause = b.AddLit("status.pause", Glyphs.StatusPause, Token.LcdText, Token.LcdGlow);
        art.StatusStop = b.AddLit("status.stop", Glyphs.StatusStop, Token.LcdText, Token.LcdGlow);

        art.RandomIndicator = AddTinyLit(b, "RDM");
        art.RepeatIndicator = AddTinyLit(b, "RPT");
        art.RepeatOneIndicator = AddTinyLit(b, "1");
        art.KbpsLabel = AddTinyLit(b, "KBPS");
        art.KhzLabel = AddTinyLit(b, "KHZ");
        art.MonoLabel = AddTinyLit(b, "MONO");
        art.StereoLabel = AddTinyLit(b, "STEREO");

        var dot = PixelFont.Dot;
        var lit = new Dictionary<char, LitSprite>();
        foreach (var ch in dot.Characters)
        {
            lit[ch] = b.AddLit($"dot.{(int)ch:X4}", dot.Glyph(ch), Token.LcdText, Token.LcdGlow);
        }

        art.DotFont = new FontSprites(dot, lit);
        art.Atlas = b.Build();
        return art;
    }

    // ---- Background --------------------------------------------------------

    private static void DrawBackground(SpriteCanvas c)
    {
        // Brushed-metal face plate: a gentle top-to-bottom falloff, a per-row
        // streak, and a little per-pixel grain. Deterministic, so every build
        // draws the same plate.
        for (var y = 0; y < c.Height; y++)
        {
            var streak = (Hash(0, y) & 7) - 3;
            for (var x = 0; x < c.Width; x++)
            {
                var grain = (Hash(x, y) & 3) - 1;
                var lum = 150 - ((y - 14) * 0.35) + (streak * 2) + grain;
                c.Plot(x, y, Token.BezelFace, ClampLum(lum));
            }
        }

        // Outer edge of the case.
        var body = new PixelRect(0, MainLayout.TitleBar.Height, c.Width, c.Height - MainLayout.TitleBar.Height);
        c.Bevel(body, Token.BezelEdgeLight, 255, Token.BezelEdgeDark, 255);
        c.HLine(1, body.Y, c.Width - 2, Token.BezelEdgeLight, 200);

        Lcd(c, MainLayout.LeftLcd);
        Lcd(c, MainLayout.RightLcd);

        // Ghost segments: every segment the LCD has is faintly visible when
        // unlit, as on the real thing. The renderer lights them on top.
        GhostDigit(c, MainLayout.Sign, "g");
        foreach (var cell in MainLayout.Digits)
        {
            GhostDigit(c, cell, SevenSegment.All);
        }

        GhostTiny(c, "RDM", MainLayout.RandomIndicator);
        GhostTiny(c, "RPT", MainLayout.RepeatIndicator);
        GhostTiny(c, "1", MainLayout.RepeatOneIndicator);
        GhostTiny(c, "KBPS", MainLayout.KbpsLabel);
        GhostTiny(c, "KHZ", MainLayout.KhzLabel);
        GhostTiny(c, "MONO", MainLayout.MonoLabel);
        GhostTiny(c, "STEREO", MainLayout.StereoLabel);

        // Visualiser floor: one dim dash per spectrum bar, so the panel reads
        // as a display even with nothing playing.
        var vis = MainLayout.Visualizer;
        var bar = vis.Width / Layout.WindowMetrics.SpectrumBars;
        for (var i = 0; i < Layout.WindowMetrics.SpectrumBars; i++)
        {
            c.HLine(vis.X + (i * bar), vis.Bottom - 1, bar - 1, Token.LcdTextDim);
        }

        // Slider grooves.
        Groove(c, new PixelRect(MainLayout.Volume.X, MainLayout.Volume.Y + 4, MainLayout.Volume.Width, 5));
        Groove(c, new PixelRect(MainLayout.Balance.X, MainLayout.Balance.Y + 4, MainLayout.Balance.Width, 5));
        Groove(c, new PixelRect(MainLayout.Seek.X, MainLayout.Seek.Y + 2, MainLayout.Seek.Width, 6));

        // Centre detent marks on the balance slider.
        var centre = MainLayout.Balance.X + (MainLayout.Balance.Width / 2);
        c.Plot(centre, MainLayout.Balance.Y, Token.BezelEdgeLight);
        c.Plot(centre, MainLayout.Balance.Bottom - 1, Token.BezelEdgeLight);

        // The seam between display and transport, like a face plate split line.
        c.HLine(8, 84, c.Width - 16, Token.BezelEdgeDark);
        c.HLine(8, 85, c.Width - 16, Token.BezelEdgeLight, 150);

        // The badge: a little lit spectrum, in the illumination colour.
        var logo = MainLayout.Logo;
        int[] heights = [6, 11, 15, 9, 13];
        for (var i = 0; i < heights.Length; i++)
        {
            c.Fill(logo.X + 1 + (i * 3), logo.Bottom - heights[i], 2, heights[i], Token.ButtonActive);
        }
    }

    /// <summary>A recessed LCD window: dark top-left lip, light bottom-right, glass inside.</summary>
    private static void Lcd(SpriteCanvas c, PixelRect r)
    {
        c.Bevel(r, Token.BezelEdgeDark, 255, Token.BezelEdgeLight, 170);
        var glass = r.Inflate(-1);
        c.Fill(glass, Token.LcdBackground);

        // The bezel's shadow falls on the top of the glass.
        c.HLine(glass.X, glass.Y, glass.Width, Token.LcdBackground, 110);
        c.HLine(glass.X, glass.Y + 1, glass.Width, Token.LcdBackground, 185);
        c.VLine(glass.X, glass.Y, glass.Height, Token.LcdBackground, 150);
    }

    private static void Groove(SpriteCanvas c, PixelRect r)
    {
        c.Bevel(r, Token.BezelEdgeDark, 255, Token.BezelEdgeLight, 170);
        c.Fill(r.Inflate(-1), Token.SliderTrack);
    }

    private static void GhostDigit(SpriteCanvas c, PixelRect cell, string segments)
        => c.Pattern(cell.X, cell.Y, SevenSegment.Pattern(segments), Token.LcdTextDim);

    private static void GhostTiny(SpriteCanvas c, string text, (int X, int Y) at)
        => DrawTiny(c, text, at.X, at.Y, Token.LcdTextDim, 255);

    // ---- Title bar ---------------------------------------------------------

    private static void DrawTitleBar(SpriteCanvas c, Token token, byte textLum)
    {
        c.GradientV(0, 0, c.Width, c.Height - 1, token, 215, 160);
        c.HLine(0, 0, c.Width, token, 255);
        c.HLine(0, c.Height - 1, c.Width, Token.BezelEdgeDark);

        const string title = "HEADUNIT";
        var textWidth = PixelFont.Tiny.Measure(title);
        var textX = (c.Width - textWidth) / 2;

        // Grooves either side of the name, stopping short of the buttons.
        var grooveEnd = MainLayout.Minimize.X - 4;
        for (var row = 4; row <= 9; row++)
        {
            var lum = (byte)(row % 2 == 0 ? 105 : 255);
            c.HLine(5, row, textX - 5 - 5, token, lum);
            c.HLine(textX + textWidth + 5, row, grooveEnd - (textX + textWidth + 5), token, lum);
        }

        DrawTiny(c, title, textX + 1, 5, token, 90);
        DrawTiny(c, title, textX, 4, Token.ButtonGlyph, textLum);
    }

    // ---- Buttons -----------------------------------------------------------

    private static ButtonSprites AddButton(SpriteAtlasBuilder b, string name, PixelRect rect, string[] glyph)
        => new(
            b.Add(name, rect.Width, rect.Height, c => DrawButton(c, glyph, pressed: false)),
            b.Add(name + ".pressed", rect.Width, rect.Height, c => DrawButton(c, glyph, pressed: true)));

    private static ButtonSprites AddLabelButton(SpriteAtlasBuilder b, string name, PixelRect rect, string label, bool lit)
        => new(
            b.Add(name, rect.Width, rect.Height, c => DrawLabelButton(c, label, lit, pressed: false)),
            b.Add(name + ".pressed", rect.Width, rect.Height, c => DrawLabelButton(c, label, lit, pressed: true)));

    private static void DrawButtonBody(SpriteCanvas c, bool pressed)
    {
        var w = c.Width;
        var h = c.Height;
        c.Frame(new PixelRect(0, 0, w, h), Token.BezelEdgeDark);

        if (pressed)
        {
            c.GradientV(1, 1, w - 2, h - 2, Token.ButtonFace, 70, 140);
            c.HLine(1, 1, w - 2, Token.ButtonFace, 25);
            c.VLine(1, 1, h - 2, Token.ButtonFace, 35);
            c.HLine(1, h - 2, w - 2, Token.ButtonFace, 150);
            c.VLine(w - 2, 2, h - 3, Token.ButtonFace, 140);
        }
        else
        {
            c.GradientV(1, 1, w - 2, h - 2, Token.ButtonFace, 215, 105);
            c.HLine(1, 1, w - 2, Token.ButtonFace, 255);
            c.VLine(1, 1, h - 2, Token.ButtonFace, 240);
            c.HLine(1, h - 2, w - 2, Token.ButtonFace, 45);
            c.VLine(w - 2, 2, h - 3, Token.ButtonFace, 60);
        }
    }

    private static void DrawButton(SpriteCanvas c, string[] glyph, bool pressed)
    {
        DrawButtonBody(c, pressed);

        var gw = glyph.Max(r => r.Length);
        var gh = glyph.Length;
        var push = pressed ? 1 : 0;
        var gx = ((c.Width - gw) / 2) + push;
        var gy = ((c.Height - gh) / 2) + push;

        // Engraved: a dark shadow down-right of the glyph, then the glyph.
        c.Pattern(gx + 1, gy + 1, glyph, Token.ButtonFace, 20);
        c.Pattern(gx, gy, glyph, Token.ButtonGlyph);
    }

    private static void DrawLabelButton(SpriteCanvas c, string label, bool lit, bool pressed)
    {
        DrawButtonBody(c, pressed);

        var push = pressed ? 1 : 0;
        var x = ((c.Width - PixelFont.Tiny.Measure(label) + 1) / 2) + push;
        var y = ((c.Height - PixelFont.Tiny.GlyphHeight) / 2) + push;

        DrawTiny(c, label, x + 1, y + 1, Token.ButtonFace, 20);
        DrawTiny(c, label, x, y, lit ? Token.ButtonActive : Token.ButtonGlyph, lit ? (byte)255 : (byte)205);
    }

    private static void DrawThumb(SpriteCanvas c, bool pressed)
    {
        var w = c.Width;
        var h = c.Height;
        c.Frame(new PixelRect(0, 0, w, h), Token.BezelEdgeDark);
        c.GradientV(1, 1, w - 2, h - 2, Token.SliderThumb, pressed ? (byte)255 : (byte)230, pressed ? (byte)150 : (byte)115);
        c.HLine(1, 1, w - 2, Token.SliderThumb, 255);
        c.HLine(1, h - 2, w - 2, Token.SliderThumb, 40);
        c.VLine(w - 2, 2, h - 3, Token.SliderThumb, 70);

        // Grip ridges.
        var cx = w / 2;
        for (var i = -1; i <= 1; i++)
        {
            c.VLine(cx + (i * 2) - 1, 3, h - 6, Token.SliderThumb, 35);
            c.VLine(cx + (i * 2), 3, h - 6, Token.SliderThumb, 255);
        }
    }

    // ---- Text --------------------------------------------------------------

    private static LitSprite AddTinyLit(SpriteAtlasBuilder b, string text)
        => b.AddLit("tiny." + text, RenderRows(PixelFont.Tiny, text), Token.LcdText, Token.LcdGlow);

    private static void DrawTiny(SpriteCanvas c, string text, int x, int y, Token token, byte lum)
        => c.Pattern(x, y, RenderRows(PixelFont.Tiny, text), token, lum);

    /// <summary>Lays a whole string out as one bitmap, for text baked into a sprite.</summary>
    private static string[] RenderRows(PixelFont font, string text)
    {
        var folded = font.Fold(text);
        var rows = new string[font.GlyphHeight];
        for (var row = 0; row < rows.Length; row++)
        {
            rows[row] = string.Join('.', folded.Select(ch => font.Glyph(ch)[row]));
        }

        return rows;
    }

    // ---- Helpers -----------------------------------------------------------

    private static byte ClampLum(double lum) => (byte)Math.Clamp((int)Math.Round(lum), 0, 255);

    /// <summary>A small integer hash, so texture is random-looking but identical every run.</summary>
    private static int Hash(int x, int y)
    {
        unchecked
        {
            var h = (uint)((x * 374761393) + (y * 668265263));
            h = (h ^ (h >> 13)) * 1274126177u;
            return (int)(h ^ (h >> 16));
        }
    }
}

/// <summary>
/// The 9x13 seven-segment cell. Each segment is drawn with tapered ends, so
/// adjacent segments meet at a hairline gap exactly as on a real LCD.
/// </summary>
public static class SevenSegment
{
    public const string All = "abcdefg";

    private static readonly string[] Map =
    [
        ".aaaaaaa.",
        "f.aaaaa.b",
        "ff.....bb",
        "ff.....bb",
        "ff.....bb",
        "f.ggggg.b",
        ".ggggggg.",
        "e.ggggg.c",
        "ee.....cc",
        "ee.....cc",
        "ee.....cc",
        "e.ddddd.c",
        ".ddddddd.",
    ];

    private static readonly string[] DigitSegments =
    [
        "abcdef", "bc", "abdeg", "abcdg", "bcfg", "acdfg", "acdefg", "abc", "abcdefg", "abcdfg",
    ];

    public static int Width => Map[0].Length;

    public static int Height => Map.Length;

    public static string ForDigit(int digit) => DigitSegments[digit];

    /// <summary>The cell with only <paramref name="segments"/> set, as '#'/'.' rows.</summary>
    public static string[] Pattern(string segments)
        => Map.Select(row => new string(row.Select(ch => ch != '.' && segments.Contains(ch) ? '#' : '.').ToArray()))
            .ToArray();
}

internal static class Glyphs
{
    public static readonly string[] Previous =
    [
        "#....#",
        "#...##",
        "#..###",
        "#.####",
        "#..###",
        "#...##",
        "#....#",
    ];

    public static readonly string[] Next =
    [
        "#....#",
        "##...#",
        "###..#",
        "####.#",
        "###..#",
        "##...#",
        "#....#",
    ];

    public static readonly string[] Play =
    [
        "#...",
        "##..",
        "###.",
        "####",
        "###.",
        "##..",
        "#...",
    ];

    public static readonly string[] Pause =
    [
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
    ];

    public static readonly string[] Stop =
    [
        "######",
        "######",
        "######",
        "######",
        "######",
        "######",
    ];

    public static readonly string[] Eject =
    [
        "...#...",
        "..###..",
        ".#####.",
        "#######",
        ".......",
        "#######",
    ];

    public static readonly string[] Minimize =
    [
        ".....",
        ".....",
        ".....",
        ".....",
        "#####",
    ];

    public static readonly string[] Close =
    [
        "#...#",
        ".#.#.",
        "..#..",
        ".#.#.",
        "#...#",
    ];

    public static readonly string[] Colon =
    [
        "..",
        "..",
        "..",
        "##",
        "##",
        "..",
        "..",
        "..",
        "##",
        "##",
        "..",
        "..",
        "..",
    ];

    public static readonly string[] StatusPlay =
    [
        "#......",
        "###....",
        "#####..",
        "#######",
        "#####..",
        "###....",
        "#......",
    ];

    public static readonly string[] StatusPause =
    [
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
        "##.##",
    ];

    public static readonly string[] StatusStop =
    [
        "#######",
        "#######",
        "#######",
        "#######",
        "#######",
        "#######",
        "#######",
    ];
}
