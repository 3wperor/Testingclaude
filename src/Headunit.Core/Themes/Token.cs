namespace Headunit.Core.Themes;

/// <summary>
/// Every colour in the chrome. Each atlas pixel names one of these; the theme
/// says what colour it is. The names (see <see cref="Tokens.Name"/>) are the
/// keys in a <c>.rtheme.json</c> file and match docs/PLAN.md §4.
/// </summary>
public enum Token : byte
{
    /// <summary>Transparent; never drawn.</summary>
    None = 0,
    BezelFace,
    BezelEdgeLight,
    BezelEdgeDark,
    LcdBackground,
    LcdText,
    LcdTextDim,
    LcdGlow,
    ButtonFace,
    ButtonGlyph,
    ButtonActive,
    SliderTrack,
    SliderThumb,
    SpectrumLow,
    SpectrumHigh,
    SpectrumPeak,
    TitlebarActive,
    TitlebarInactive,
}

public static class Tokens
{
    private static readonly (Token Token, string Name)[] Table =
    [
        (Token.BezelFace, "bezel.face"),
        (Token.BezelEdgeLight, "bezel.edge.light"),
        (Token.BezelEdgeDark, "bezel.edge.dark"),
        (Token.LcdBackground, "lcd.background"),
        (Token.LcdText, "lcd.text"),
        (Token.LcdTextDim, "lcd.text.dim"),
        (Token.LcdGlow, "lcd.glow"),
        (Token.ButtonFace, "button.face"),
        (Token.ButtonGlyph, "button.glyph"),
        (Token.ButtonActive, "button.active"),
        (Token.SliderTrack, "slider.track"),
        (Token.SliderThumb, "slider.thumb"),
        (Token.SpectrumLow, "spectrum.low"),
        (Token.SpectrumHigh, "spectrum.high"),
        (Token.SpectrumPeak, "spectrum.peak"),
        (Token.TitlebarActive, "titlebar.active"),
        (Token.TitlebarInactive, "titlebar.inactive"),
    ];

    /// <summary>Every real token, in declaration order (excludes <see cref="Token.None"/>).</summary>
    public static IReadOnlyList<Token> All { get; } = Table.Select(t => t.Token).ToArray();

    /// <summary>Size of a lookup table indexed by token byte value.</summary>
    public static int Count { get; } = Enum.GetValues<Token>().Length;

    public static string Name(Token token)
    {
        foreach (var (t, name) in Table)
        {
            if (t == token)
            {
                return name;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(token), token, "Not a themable token.");
    }

    public static bool TryParse(string name, out Token token)
    {
        foreach (var (t, n) in Table)
        {
            if (string.Equals(n, name, StringComparison.Ordinal))
            {
                token = t;
                return true;
            }
        }

        token = Token.None;
        return false;
    }
}
