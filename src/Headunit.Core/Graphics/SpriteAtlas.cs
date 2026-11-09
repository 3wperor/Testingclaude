namespace Headunit.Core.Graphics;

/// <summary>A named rectangle in the atlas.</summary>
public readonly record struct Sprite(int Id, PixelRect Source)
{
    public int Width => Source.Width;

    public int Height => Source.Height;
}

/// <summary>
/// A glowing LCD element: the lit pixels, plus a soft halo one pixel larger on
/// every side. Text draws every halo before any core so neighbouring glyphs'
/// glow never washes over each other's strokes.
/// </summary>
public readonly record struct LitSprite(Sprite Core, Sprite Halo);

/// <summary>Every sprite packed into one mask image.</summary>
public sealed class SpriteAtlas
{
    private readonly Dictionary<string, Sprite> _byName;

    internal SpriteAtlas(MaskImage mask, IReadOnlyList<(string Name, Sprite Sprite)> sprites)
    {
        Mask = mask;
        Sprites = sprites;
        _byName = sprites.ToDictionary(s => s.Name, s => s.Sprite, StringComparer.Ordinal);
    }

    public MaskImage Mask { get; }

    public IReadOnlyList<(string Name, Sprite Sprite)> Sprites { get; }

    public Sprite this[string name] => _byName[name];

    public bool TryGet(string name, out Sprite sprite) => _byName.TryGetValue(name, out sprite);
}

/// <summary>
/// Packs sprites into rows ("shelves") as they are added. Simple, deterministic,
/// and plenty for an atlas this size.
/// </summary>
public sealed class SpriteAtlasBuilder
{
    /// <summary>Transparent gutter between sprites, so no blit can ever bleed into a neighbour.</summary>
    private const int Gutter = 1;

    private readonly int _width;
    private readonly List<(string Name, Sprite Sprite, MaskImage Pixels)> _entries = [];
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);
    private int _cursorX;
    private int _shelfY;
    private int _shelfHeight;

    public SpriteAtlasBuilder(int width = 512)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        _width = width;
    }

    public Sprite Add(string name, int width, int height, Action<SpriteCanvas> draw)
    {
        if (!_names.Add(name))
        {
            throw new ArgumentException($"A sprite named '{name}' already exists.", nameof(name));
        }

        if (width > _width)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"Sprite '{name}' is wider than the atlas.");
        }

        var canvas = new SpriteCanvas(width, height);
        draw(canvas);

        if (_cursorX + width > _width)
        {
            _shelfY += _shelfHeight + Gutter;
            _cursorX = 0;
            _shelfHeight = 0;
        }

        var sprite = new Sprite(_entries.Count, new PixelRect(_cursorX, _shelfY, width, height));
        _entries.Add((name, sprite, canvas.Mask));

        _cursorX += width + Gutter;
        _shelfHeight = Math.Max(_shelfHeight, height);
        return sprite;
    }

    /// <summary>
    /// Adds a lit element and its glow halo from a single boolean core shape.
    /// Halo alpha falls off with distance; the theme's glow strength scales it
    /// again at bake time.
    /// </summary>
    public LitSprite AddLit(string name, IReadOnlyList<string> rows, Themes.Token core, Themes.Token glow)
    {
        var height = rows.Count;
        var width = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        bool On(int x, int y) => y >= 0 && y < rows.Count && x >= 0 && x < rows[y].Length && rows[y][x] == '#';

        var coreSprite = Add(name, width, height, c => c.Pattern(0, 0, rows, core));
        var haloSprite = Add(name + ".halo", width + 2, height + 2, c =>
        {
            for (var y = -1; y <= height; y++)
            {
                for (var x = -1; x <= width; x++)
                {
                    var weight = 0.0;
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (!On(x + dx, y + dy))
                            {
                                continue;
                            }

                            var w = (dx, dy) switch
                            {
                                (0, 0) => 1.0,
                                (0, _) or (_, 0) => 0.6,
                                _ => 0.3,
                            };
                            weight = Math.Max(weight, w);
                        }
                    }

                    if (weight > 0)
                    {
                        c.Plot(x + 1, y + 1, glow, 255, (byte)Math.Round(weight * 255));
                    }
                }
            }
        });

        return new LitSprite(coreSprite, haloSprite);
    }

    public SpriteAtlas Build()
    {
        var height = _shelfY + _shelfHeight;
        var mask = new MaskImage(_width, Math.Max(height, 1));

        foreach (var (_, sprite, pixels) in _entries)
        {
            pixels.CopyTo(mask, sprite.Source.X, sprite.Source.Y);
        }

        return new SpriteAtlas(mask, _entries.Select(e => (e.Name, e.Sprite)).ToList());
    }
}
