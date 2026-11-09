using Headunit.Core.Ui;
using Xunit;

namespace Headunit.Core.Tests;

public class PixelFontTests
{
    public static TheoryData<string> Fonts => new() { "dot", "tiny" };

    private static PixelFont Get(string name) => name == "dot" ? PixelFont.Dot : PixelFont.Tiny;

    [Theory]
    [MemberData(nameof(Fonts))]
    public void Covers_uppercase_and_digits(string name)
    {
        var font = Get(name);
        foreach (var c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .-:")
        {
            Assert.True(font.Has(c), $"{font.Name} lacks '{c}'");
        }
    }

    [Fact]
    public void Dot_font_covers_the_punctuation_titles_actually_use()
    {
        foreach (var c in "()[]&'\",!?/+#%*_")
        {
            Assert.True(PixelFont.Dot.Has(c), $"missing '{c}'");
        }
    }

    [Theory]
    [MemberData(nameof(Fonts))]
    public void Every_glyph_has_at_least_one_lit_pixel_except_space(string name)
    {
        var font = Get(name);
        foreach (var c in font.Characters.Where(c => c != ' '))
        {
            Assert.Contains(font.Glyph(c), row => row.Contains('#'));
        }
    }

    [Fact]
    public void Measure_excludes_the_trailing_spacing_column()
    {
        Assert.Equal(0, PixelFont.Dot.Measure(string.Empty));
        Assert.Equal(5, PixelFont.Dot.Measure("A"));
        Assert.Equal(11, PixelFont.Dot.Measure("AB"));
        Assert.Equal(7, PixelFont.Tiny.Measure("AB"));
    }

    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("Björk – Jóga", "BJORK - JOGA")]
    [InlineData("Straße", "STRASSE")]
    [InlineData("Sigur Rós…", "SIGUR ROS...")]
    [InlineData("Don’t Stop", "DON'T STOP")]
    [InlineData("東京", "??")]
    [InlineData("tab\there", "TAB HERE")]
    public void Fold_rewrites_text_into_what_the_lcd_can_show(string input, string expected)
        => Assert.Equal(expected, PixelFont.Dot.Fold(input));

    [Fact]
    public void Fold_output_is_always_drawable()
    {
        var folded = PixelFont.Tiny.Fold("Ünïcödé & {braces} ~ 東");
        Assert.All(folded, c => Assert.True(PixelFont.Tiny.Has(c), $"'{c}'"));
    }
}
