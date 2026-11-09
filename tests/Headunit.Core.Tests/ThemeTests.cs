using Headunit.Core.Graphics;
using Headunit.Core.Themes;
using Xunit;

namespace Headunit.Core.Tests;

public class ThemeTests
{
    public static TheoryData<string> PresetNames => new(ThemePresets.All.Select(t => t.Name));

    [Theory]
    [MemberData(nameof(PresetNames))]
    public void Every_preset_defines_every_token(string name)
    {
        var theme = ThemePresets.All.Single(t => t.Name == name);
        Assert.All(Tokens.All, t => Assert.True(theme.Values.ContainsKey(t), Tokens.Name(t)));
    }

    [Fact]
    public void A_theme_missing_a_token_is_refused_by_name()
    {
        var values = ThemePresets.Default.Values.Where(kv => kv.Key != Token.LcdGlow).ToDictionary();
        var ex = Assert.Throws<ArgumentException>(() => new Theme("Broken", values));
        Assert.Contains("lcd.glow", ex.Message);
    }

    [Fact]
    public void Token_names_are_unique_and_round_trip()
    {
        var names = Tokens.All.Select(Tokens.Name).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());

        foreach (var token in Tokens.All)
        {
            Assert.True(Tokens.TryParse(Tokens.Name(token), out var parsed));
            Assert.Equal(token, parsed);
        }

        Assert.False(Tokens.TryParse("lcd.nope", out _));
    }

    [Fact]
    public void Presets_are_actually_different_colourways()
    {
        var lcdColours = ThemePresets.All
            .Select(t => ((FlatToken)t[Token.LcdText]).Color)
            .ToList();

        Assert.Equal(lcdColours.Count, lcdColours.Distinct().Count());
    }

    [Fact]
    public void Ghost_segments_stay_much_darker_than_lit_ones()
    {
        // If dim gets close to lit, unlit segments read as lit and the clock
        // shows 88:88. Keep it well apart in every preset.
        foreach (var theme in ThemePresets.All)
        {
            var lit = Luma(((FlatToken)theme[Token.LcdText]).Color);
            var dim = Luma(((FlatToken)theme[Token.LcdTextDim]).Color);
            Assert.True(dim * 3 < lit, theme.Name);
        }

        static double Luma(Rgb c) => (0.2126 * c.R) + (0.7152 * c.G) + (0.0722 * c.B);
    }
}
