using Headunit.Core.Themes;

namespace Headunit.Core.Graphics;

/// <summary>
/// The source form of all artwork: per pixel, a luminance, an alpha, and the
/// theme <see cref="Token"/> that pixel belongs to. No colour is stored
/// anywhere — colour is applied when a theme is baked, which is the whole trick
/// that lets one set of art wear any illumination.
/// </summary>
public sealed class MaskImage
{
    public MaskImage(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        Luminance = new byte[width * height];
        Alpha = new byte[width * height];
        Tokens = new byte[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Luminance { get; }

    public byte[] Alpha { get; }

    /// <summary>One <see cref="Token"/> per pixel, stored as its byte value.</summary>
    public byte[] Tokens { get; }

    public PixelRect Bounds => new(0, 0, Width, Height);

    public void Set(int x, int y, Token token, byte luminance, byte alpha = 255)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        var i = (y * Width) + x;
        Luminance[i] = luminance;
        Alpha[i] = alpha;
        Tokens[i] = (byte)token;
    }

    public (Token Token, byte Luminance, byte Alpha) Get(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return (Token.None, 0, 0);
        }

        var i = (y * Width) + x;
        return ((Token)Tokens[i], Luminance[i], Alpha[i]);
    }

    public void CopyTo(MaskImage destination, int dx, int dy)
    {
        for (var y = 0; y < Height; y++)
        {
            var ty = dy + y;
            if ((uint)ty >= (uint)destination.Height)
            {
                continue;
            }

            for (var x = 0; x < Width; x++)
            {
                var tx = dx + x;
                if ((uint)tx >= (uint)destination.Width)
                {
                    continue;
                }

                var s = (y * Width) + x;
                var d = (ty * destination.Width) + tx;
                destination.Luminance[d] = Luminance[s];
                destination.Alpha[d] = Alpha[s];
                destination.Tokens[d] = Tokens[s];
            }
        }
    }

    /// <summary>
    /// The mask as an image a person can open: luminance in red, token index in
    /// green, alpha as alpha. This is the byte-for-byte interchange form the
    /// macOS port reads.
    /// </summary>
    public Surface ToInterchangeImage()
    {
        var image = new Surface(Width, Height);
        for (var i = 0; i < Luminance.Length; i++)
        {
            image.Pixels[i] = ((uint)Alpha[i] << 24) | ((uint)Luminance[i] << 16) | ((uint)Tokens[i] << 8);
        }

        return image;
    }
}
