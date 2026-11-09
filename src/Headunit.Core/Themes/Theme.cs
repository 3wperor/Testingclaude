using Headunit.Core.Graphics;

namespace Headunit.Core.Themes;

public abstract record TokenValue;

/// <summary>A single colour. Art at full luminance shows exactly this; darker luminance shades it.</summary>
public sealed record FlatToken(Rgb Color) : TokenValue;

/// <summary>A three-stop shading ramp for surfaces with real form — bezel, buttons, thumbs.</summary>
public sealed record RampToken(Rgb Shadow, Rgb Mid, Rgb High) : TokenValue;

/// <summary>The LCD's light bleed: a colour plus how strongly it shows (0 to 1).</summary>
public sealed record GlowToken(Rgb Color, double Strength) : TokenValue;

/// <summary>
/// A complete colourway. Construction fails if any token is missing, so a theme
/// that renders is a theme that renders <em>everything</em>.
/// </summary>
public sealed class Theme
{
    private readonly Dictionary<Token, TokenValue> _values;

    public Theme(string name, IReadOnlyDictionary<Token, TokenValue> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var missing = Tokens.All.Where(t => !values.ContainsKey(t)).Select(Tokens.Name).ToList();
        if (missing.Count > 0)
        {
            throw new ArgumentException($"Theme '{name}' is missing: {string.Join(", ", missing)}.", nameof(values));
        }

        Name = name;
        _values = new Dictionary<Token, TokenValue>(values);
    }

    public string Name { get; }

    public IReadOnlyDictionary<Token, TokenValue> Values => _values;

    public TokenValue this[Token token] => _values[token];
}
