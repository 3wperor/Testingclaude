namespace Headunit.Core.Graphics;

/// <summary>An axis-aligned rectangle in whole art pixels. Right and Bottom are exclusive.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(int x, int y) => x >= X && y >= Y && x < Right && y < Bottom;

    public bool Contains(PixelRect other)
        => other.X >= X && other.Y >= Y && other.Right <= Right && other.Bottom <= Bottom;

    public bool IntersectsWith(PixelRect other)
        => other.X < Right && X < other.Right && other.Y < Bottom && Y < other.Bottom;

    public PixelRect Intersect(PixelRect other)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        return right > x && bottom > y ? new PixelRect(x, y, right - x, bottom - y) : default;
    }

    public PixelRect Offset(int dx, int dy) => this with { X = X + dx, Y = Y + dy };

    public PixelRect Inflate(int by) => new(X - by, Y - by, Width + (2 * by), Height + (2 * by));
}
