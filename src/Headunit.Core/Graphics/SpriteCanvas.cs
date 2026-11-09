using Headunit.Core.Themes;

namespace Headunit.Core.Graphics;

/// <summary>
/// Drawing primitives for authoring one sprite. Coordinates are local to the
/// sprite and everything is clipped to it, so art code never has to care where
/// in the atlas the sprite ends up.
/// </summary>
public sealed class SpriteCanvas
{
    internal SpriteCanvas(int width, int height) => Mask = new MaskImage(width, height);

    internal MaskImage Mask { get; }

    public int Width => Mask.Width;

    public int Height => Mask.Height;

    public void Plot(int x, int y, Token token, byte luminance = 255, byte alpha = 255)
        => Mask.Set(x, y, token, luminance, alpha);

    public (Token Token, byte Luminance, byte Alpha) Get(int x, int y) => Mask.Get(x, y);

    public void Fill(int x, int y, int width, int height, Token token, byte luminance = 255)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var col = x; col < x + width; col++)
            {
                Mask.Set(col, row, token, luminance);
            }
        }
    }

    public void Fill(PixelRect rect, Token token, byte luminance = 255)
        => Fill(rect.X, rect.Y, rect.Width, rect.Height, token, luminance);

    public void HLine(int x, int y, int width, Token token, byte luminance = 255)
        => Fill(x, y, width, 1, token, luminance);

    public void VLine(int x, int y, int height, Token token, byte luminance = 255)
        => Fill(x, y, 1, height, token, luminance);

    /// <summary>Vertical luminance gradient, top row to bottom row inclusive.</summary>
    public void GradientV(int x, int y, int width, int height, Token token, byte top, byte bottom)
    {
        for (var row = 0; row < height; row++)
        {
            var t = height == 1 ? 0.0 : row / (double)(height - 1);
            var lum = (byte)Math.Round(top + ((bottom - top) * t));
            HLine(x, y + row, width, token, lum);
        }
    }

    /// <summary>
    /// A one-pixel frame: <paramref name="topLeft"/> on the top and left edges,
    /// <paramref name="bottomRight"/> on the bottom and right — swap them and a
    /// raised edge becomes a sunken one.
    /// </summary>
    public void Bevel(PixelRect rect, Token topLeft, byte topLeftLum, Token bottomRight, byte bottomRightLum)
    {
        HLine(rect.X, rect.Y, rect.Width, topLeft, topLeftLum);
        VLine(rect.X, rect.Y, rect.Height, topLeft, topLeftLum);
        HLine(rect.X, rect.Bottom - 1, rect.Width, bottomRight, bottomRightLum);
        VLine(rect.Right - 1, rect.Y, rect.Height, bottomRight, bottomRightLum);
    }

    public void Frame(PixelRect rect, Token token, byte luminance = 255)
        => Bevel(rect, token, luminance, token, luminance);

    /// <summary>Plots every <c>#</c> in a row-per-string bitmap.</summary>
    public void Pattern(int x, int y, IReadOnlyList<string> rows, Token token, byte luminance = 255)
    {
        for (var row = 0; row < rows.Count; row++)
        {
            var line = rows[row];
            for (var col = 0; col < line.Length; col++)
            {
                if (line[col] == '#')
                {
                    Mask.Set(x + col, y + row, token, luminance);
                }
            }
        }
    }
}
