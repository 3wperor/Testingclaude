using Headunit.Core.Themes;

namespace Headunit.Core.Graphics;

/// <summary>
/// The atlas with a theme applied: real colour, ready to blit. Built once per
/// theme change, never per frame — after this, drawing is plain copies.
/// </summary>
public sealed class TintedAtlas
{
    private readonly uint[][] _luts;

    private TintedAtlas(Theme theme, int width, int height, uint[] pixels, uint[][] luts)
    {
        Theme = theme;
        Width = width;
        Height = height;
        Pixels = pixels;
        _luts = luts;
    }

    public Theme Theme { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>0xAARRGGBB, row-major.</summary>
    public uint[] Pixels { get; }

    /// <summary>The theme colour a token takes at a given luminance, fully opaque.</summary>
    public uint Color(Token token, byte luminance = 255) => _luts[(int)token][luminance] | 0xFF000000;

    public static TintedAtlas Bake(SpriteAtlas atlas, Theme theme)
    {
        var luts = new uint[Tokens.Count][];
        var alphaScale = new double[Tokens.Count];
        luts[(int)Token.None] = new uint[256];

        foreach (var token in Tokens.All)
        {
            var (lut, alpha) = TokenLut.Build(theme[token]);
            luts[(int)token] = lut;
            alphaScale[(int)token] = alpha;
        }

        var mask = atlas.Mask;
        var pixels = new uint[mask.Width * mask.Height];

        for (var i = 0; i < pixels.Length; i++)
        {
            var token = mask.Tokens[i];
            var a = mask.Alpha[i];
            if (a == 0 || token == (byte)Token.None || token >= Tokens.Count)
            {
                continue;
            }

            var alpha = (uint)Math.Round(a * alphaScale[token]);
            pixels[i] = (luts[token][mask.Luminance[i]] & 0x00FFFFFF) | (Math.Min(alpha, 255u) << 24);
        }

        return new TintedAtlas(theme, mask.Width, mask.Height, pixels, luts);
    }
}

/// <summary>
/// Maps a token's value to a 256-entry luminance → colour table.
/// <list type="bullet">
/// <item>A <b>ramp</b> runs shadow (0) → mid (128) → high (255).</item>
/// <item>A <b>flat</b> colour runs black (0) → the colour (255), so art drawn at
/// full luminance lands exactly on the theme author's hex value, and darker
/// luminance shades it.</item>
/// <item>A <b>glow</b> is flat, plus an alpha multiplier — its strength.</item>
/// </list>
/// </summary>
public static class TokenLut
{
    public const int MidPoint = 128;

    public static (uint[] Lut, double AlphaScale) Build(TokenValue value)
    {
        var lut = new uint[256];
        switch (value)
        {
            case RampToken ramp:
                for (var l = 0; l < 256; l++)
                {
                    var c = l <= MidPoint
                        ? Rgb.Lerp(ramp.Shadow, ramp.Mid, l / (double)MidPoint)
                        : Rgb.Lerp(ramp.Mid, ramp.High, (l - MidPoint) / (double)(255 - MidPoint));
                    lut[l] = c.ToArgb();
                }

                return (lut, 1.0);

            case FlatToken flat:
                FillFlat(lut, flat.Color);
                return (lut, 1.0);

            case GlowToken glow:
                FillFlat(lut, glow.Color);
                return (lut, Math.Clamp(glow.Strength, 0.0, 1.0));

            default:
                throw new ArgumentException($"Unknown token value type {value.GetType().Name}.", nameof(value));
        }
    }

    private static void FillFlat(uint[] lut, Rgb color)
    {
        var black = new Rgb(0, 0, 0);
        for (var l = 0; l < 256; l++)
        {
            lut[l] = Rgb.Lerp(black, color, l / 255.0).ToArgb();
        }
    }
}
