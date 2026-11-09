using System.Text;

namespace Headunit.Core.Ui;

/// <summary>
/// A fixed-cell bitmap font, uppercase only — like every head-unit LCD ever
/// made. Text is folded to what the font can show before it is drawn, so
/// "Björk – Jóga" reads as "BJORK - JOGA" rather than as boxes.
/// </summary>
public sealed class PixelFont
{
    private readonly Dictionary<char, string[]> _glyphs;

    private PixelFont(string name, int width, int height, Dictionary<char, string[]> glyphs)
    {
        Name = name;
        GlyphWidth = width;
        GlyphHeight = height;
        _glyphs = glyphs;

        foreach (var (c, rows) in glyphs)
        {
            if (rows.Length != height || rows.Any(r => r.Length != width))
            {
                throw new InvalidOperationException($"{name}: glyph '{c}' is not {width}x{height}.");
            }
        }
    }

    public string Name { get; }

    public int GlyphWidth { get; }

    public int GlyphHeight { get; }

    /// <summary>Horizontal distance between glyph origins: one blank column of spacing.</summary>
    public int Advance => GlyphWidth + 1;

    public IEnumerable<char> Characters => _glyphs.Keys;

    public IReadOnlyList<string> Glyph(char c) => _glyphs[c];

    public bool Has(char c) => _glyphs.ContainsKey(c);

    /// <summary>Pixel width of a string as drawn (no trailing spacing column).</summary>
    public int Measure(string text) => text.Length == 0 ? 0 : (text.Length * Advance) - 1;

    /// <summary>
    /// Rewrites arbitrary text into characters this font has: uppercases,
    /// strips accents from Latin letters, maps typographic punctuation to
    /// ASCII, and replaces anything else with '?'.
    /// </summary>
    public string Fold(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var raw in text)
        {
            var c = char.ToUpperInvariant(raw);
            if (_glyphs.ContainsKey(c))
            {
                sb.Append(c);
                continue;
            }

            if (TextFolding.Map.TryGetValue(c, out var folded))
            {
                foreach (var f in folded)
                {
                    sb.Append(_glyphs.ContainsKey(f) ? f : '?');
                }

                continue;
            }

            sb.Append(char.IsWhiteSpace(c) ? ' ' : (_glyphs.ContainsKey('?') ? '?' : ' '));
        }

        return sb.ToString();
    }

    /// <summary>The 5x7 dot-matrix font on the LCD.</summary>
    public static PixelFont Dot { get; } = new("dot5x7", 5, 7, Parse(DotFontData.Source));

    /// <summary>The 3x5 label font engraved on buttons and the title bar.</summary>
    public static PixelFont Tiny { get; } = new("tiny3x5", 3, 5, Parse(TinyFontData.Source));

    /// <summary>
    /// Glyph source format: a line holding just the character, then its rows.
    /// Blank lines separate glyphs. Kept as text so the art is diffable.
    /// </summary>
    private static Dictionary<char, string[]> Parse(string source)
    {
        var glyphs = new Dictionary<char, string[]>();
        var blocks = source.Replace("\r", string.Empty).Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var key = lines[0] == "SPACE" ? ' ' : lines[0][0];
            glyphs[key] = lines[1..];
        }

        return glyphs;
    }
}

internal static class TextFolding
{
    public static readonly IReadOnlyDictionary<char, string> Map = Build();

    private static Dictionary<char, string> Build()
    {
        var map = new Dictionary<char, string>();

        void Add(string from, string to)
        {
            foreach (var c in from)
            {
                map[c] = to;
                map[char.ToUpperInvariant(c)] = to;
            }
        }

        Add("ÀÁÂÃÄÅĀĂĄ", "A");
        Add("ÇĆĈĊČ", "C");
        Add("ĎĐ", "D");
        Add("ÈÉÊËĒĔĖĘĚ", "E");
        Add("ĜĞĠĢ", "G");
        Add("ĤĦ", "H");
        Add("ÌÍÎÏĨĪĬĮİ", "I");
        Add("Ĵ", "J");
        Add("Ķ", "K");
        Add("ĹĻĽĿŁ", "L");
        Add("ÑŃŅŇ", "N");
        Add("ÒÓÔÕÖØŌŎŐ", "O");
        Add("ŔŖŘ", "R");
        Add("ŚŜŞŠ", "S");
        Add("ŢŤŦ", "T");
        Add("ÙÚÛÜŨŪŬŮŰŲ", "U");
        Add("Ŵ", "W");
        Add("ÝŸŶ", "Y");
        Add("ŹŻŽ", "Z");
        Add("Æ", "AE");
        Add("Œ", "OE");
        Add("Þ", "TH");
        map['ß'] = "SS";
        map['ÿ'] = "Y";
        map['‘'] = "'";
        map['’'] = "'";
        map['‚'] = ",";
        map['“'] = "\"";
        map['”'] = "\"";
        map['„'] = "\"";
        map['–'] = "-";
        map['—'] = "-";
        map['…'] = "...";
        map['•'] = "*";
        map['·'] = "-";
        map['×'] = "X";
        return map;
    }
}
