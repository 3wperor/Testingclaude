using System.Globalization;

namespace Headunit.Core.Graphics;

/// <summary>An opaque colour as a theme author writes it: <c>#RRGGBB</c>.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Parse(string hex)
    {
        if (!TryParse(hex, out var value))
        {
            throw new FormatException($"'{hex}' is not a #RRGGBB colour.");
        }

        return value;
    }

    public static bool TryParse(string? hex, out Rgb value)
    {
        value = default;
        if (hex is null || hex.Length != 7 || hex[0] != '#')
        {
            return false;
        }

        if (!uint.TryParse(hex.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var packed))
        {
            return false;
        }

        value = new Rgb((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);
        return true;
    }

    /// <summary>Linear interpolation in sRGB space, rounded to nearest.</summary>
    public static Rgb Lerp(Rgb a, Rgb b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return new Rgb(Mix(a.R, b.R, t), Mix(a.G, b.G, t), Mix(a.B, b.B, t));

        static byte Mix(byte x, byte y, double t) => (byte)Math.Round(x + ((y - x) * t), MidpointRounding.AwayFromZero);
    }

    /// <summary>Packs as 0xAARRGGBB, which is BGRA byte order in memory on little-endian machines.</summary>
    public uint ToArgb(byte alpha = 255) => ((uint)alpha << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    public static Rgb FromArgb(uint argb) => new((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>Helpers for colours packed as 0xAARRGGBB.</summary>
public static class Argb
{
    public static byte A(uint c) => (byte)(c >> 24);

    /// <summary>Source-over compositing of <paramref name="src"/> onto <paramref name="dst"/>.</summary>
    public static uint Over(uint dst, uint src)
    {
        uint sa = src >> 24;
        if (sa == 255)
        {
            return src;
        }

        if (sa == 0)
        {
            return dst;
        }

        uint inv = 255 - sa;
        uint da = dst >> 24;

        uint r = Div255((((src >> 16) & 0xFF) * sa) + (((dst >> 16) & 0xFF) * inv));
        uint g = Div255((((src >> 8) & 0xFF) * sa) + (((dst >> 8) & 0xFF) * inv));
        uint b = Div255(((src & 0xFF) * sa) + ((dst & 0xFF) * inv));
        uint a = sa + Div255(da * inv);

        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    /// <summary>Exact round-to-nearest division by 255 for values up to 255*255*2.</summary>
    private static uint Div255(uint x) => (x + 128 + ((x + 128) >> 8)) >> 8;
}
