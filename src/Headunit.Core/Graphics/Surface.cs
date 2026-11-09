namespace Headunit.Core.Graphics;

/// <summary>
/// A frame buffer of 0xAARRGGBB pixels. On Windows this is copied straight into
/// a <c>WriteableBitmap</c> (Bgra32 is the same bytes); the preview tool writes
/// it to PNG.
/// </summary>
public sealed class Surface
{
    public Surface(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        Pixels = new uint[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public uint[] Pixels { get; }

    public PixelRect Bounds => new(0, 0, Width, Height);

    public uint this[int x, int y] => Pixels[(y * Width) + x];

    public void Clear(uint argb) => Array.Fill(Pixels, argb);

    public void Blit(TintedAtlas atlas, Sprite sprite, int x, int y) => Blit(atlas, sprite, x, y, Bounds);

    /// <summary>Draws a sprite with source-over alpha, clipped to <paramref name="clip"/>.</summary>
    public void Blit(TintedAtlas atlas, Sprite sprite, int x, int y, PixelRect clip)
    {
        var dest = new PixelRect(x, y, sprite.Width, sprite.Height).Intersect(clip).Intersect(Bounds);
        if (dest.IsEmpty)
        {
            return;
        }

        var src = atlas.Pixels;
        var srcX0 = sprite.Source.X + (dest.X - x);
        var srcY0 = sprite.Source.Y + (dest.Y - y);

        for (var row = 0; row < dest.Height; row++)
        {
            var s = ((srcY0 + row) * atlas.Width) + srcX0;
            var d = ((dest.Y + row) * Width) + dest.X;

            for (var col = 0; col < dest.Width; col++, s++, d++)
            {
                var p = src[s];
                var a = p >> 24;
                if (a == 255)
                {
                    Pixels[d] = p;
                }
                else if (a != 0)
                {
                    Pixels[d] = Argb.Over(Pixels[d], p);
                }
            }
        }
    }

    public void Blit(TintedAtlas atlas, LitSprite lit, int x, int y, PixelRect clip)
    {
        Blit(atlas, lit.Halo, x - 1, y - 1, clip);
        Blit(atlas, lit.Core, x, y, clip);
    }

    public void Blit(TintedAtlas atlas, LitSprite lit, int x, int y) => Blit(atlas, lit, x, y, Bounds);

    public void FillRect(PixelRect rect, uint argb)
    {
        var r = rect.Intersect(Bounds);
        for (var y = r.Y; y < r.Bottom; y++)
        {
            Array.Fill(Pixels, argb, (y * Width) + r.X, r.Width);
        }
    }

    /// <summary>Nearest-neighbour enlargement — the same thing the WPF host does on screen.</summary>
    public Surface Upscale(int factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);
        var big = new Surface(Width * factor, Height * factor);
        for (var y = 0; y < big.Height; y++)
        {
            var srcRow = (y / factor) * Width;
            var dstRow = y * big.Width;
            for (var x = 0; x < big.Width; x++)
            {
                big.Pixels[dstRow + x] = Pixels[srcRow + (x / factor)];
            }
        }

        return big;
    }
}
