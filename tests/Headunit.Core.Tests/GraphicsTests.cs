using Headunit.Core.Graphics;
using Headunit.Core.Themes;
using Xunit;

namespace Headunit.Core.Tests;

public class GraphicsTests
{
    [Fact]
    public void Rgb_round_trips_through_hex()
    {
        var c = Rgb.Parse("#FFA23A");
        Assert.Equal(new Rgb(0xFF, 0xA2, 0x3A), c);
        Assert.Equal("#FFA23A", c.ToString());
        Assert.Equal(0xFFFFA23Au, c.ToArgb());
    }

    [Theory]
    [InlineData("FFA23A")]
    [InlineData("#FFA23")]
    [InlineData("#GGGGGG")]
    [InlineData("")]
    public void Rgb_rejects_malformed_hex(string hex) => Assert.False(Rgb.TryParse(hex, out _));

    [Fact]
    public void Over_is_exact_at_the_alpha_extremes()
    {
        const uint dst = 0xFF102030;
        Assert.Equal(dst, Argb.Over(dst, 0x00FFFFFF));
        Assert.Equal(0xFFAABBCCu, Argb.Over(dst, 0xFFAABBCC));
    }

    [Fact]
    public void Over_blends_halfway_at_half_alpha()
    {
        var mixed = Argb.Over(0xFF000000, 0x80FFFFFF);
        Assert.Equal(255, Argb.A(mixed));
        Assert.InRange((int)((mixed >> 16) & 0xFF), 127, 129);
    }

    [Fact]
    public void PixelRect_intersection_and_containment()
    {
        var a = new PixelRect(0, 0, 10, 10);
        var b = new PixelRect(5, 5, 10, 10);
        Assert.Equal(new PixelRect(5, 5, 5, 5), a.Intersect(b));
        Assert.True(a.Intersect(new PixelRect(20, 20, 5, 5)).IsEmpty);
        Assert.True(a.Contains(9, 9));
        Assert.False(a.Contains(10, 9)); // right edge is exclusive
    }

    [Fact]
    public void Ramp_lut_hits_its_three_stops_exactly()
    {
        var ramp = new RampToken(Rgb.Parse("#000000"), Rgb.Parse("#808080"), Rgb.Parse("#FFFFFF"));
        var (lut, alpha) = TokenLut.Build(ramp);

        Assert.Equal(1.0, alpha);
        Assert.Equal(0xFF000000u, lut[0]);
        Assert.Equal(0xFF808080u, lut[TokenLut.MidPoint]);
        Assert.Equal(0xFFFFFFFFu, lut[255]);
    }

    [Fact]
    public void Flat_lut_lands_on_the_authored_colour_at_full_luminance()
    {
        var (lut, _) = TokenLut.Build(new FlatToken(Rgb.Parse("#FFA23A")));
        Assert.Equal(0xFFFFA23Au, lut[255]);
        Assert.Equal(0xFF000000u, lut[0]);
    }

    [Fact]
    public void Glow_strength_becomes_an_alpha_multiplier()
    {
        var (_, alpha) = TokenLut.Build(new GlowToken(Rgb.Parse("#FF8A00"), 0.35));
        Assert.Equal(0.35, alpha);
    }

    [Fact]
    public void Baking_maps_each_pixel_through_its_tokens_lut()
    {
        var b = new SpriteAtlasBuilder(16);
        var sprite = b.Add("s", 3, 1, c =>
        {
            c.Plot(0, 0, Token.LcdText);
            c.Plot(1, 0, Token.LcdGlow);
            // (2,0) left transparent
        });
        var tinted = TintedAtlas.Bake(b.Build(), ThemePresets.BlaupunktAmber);

        var x = sprite.Source.X;
        Assert.Equal(0xFFFFA23Au, tinted.Pixels[x]);
        Assert.Equal((uint)Math.Round(255 * 0.35), tinted.Pixels[x + 1] >> 24);
        Assert.Equal(0u, tinted.Pixels[x + 2]);
    }

    [Fact]
    public void Atlas_builder_packs_without_overlap_and_rejects_duplicate_names()
    {
        var b = new SpriteAtlasBuilder(32);
        var sprites = Enumerable.Range(0, 20).Select(i => b.Add($"s{i}", 7 + (i % 5), 3 + (i % 4), _ => { })).ToList();
        var atlas = b.Build();

        foreach (var s in sprites)
        {
            Assert.True(atlas.Mask.Bounds.Contains(s.Source));
            Assert.DoesNotContain(sprites, o => o.Id != s.Id && o.Source.IntersectsWith(s.Source));
        }

        Assert.Throws<ArgumentException>(() => b.Add("s0", 1, 1, _ => { }));
    }

    [Fact]
    public void Lit_sprite_halo_surrounds_the_core_and_is_softer_than_it()
    {
        var b = new SpriteAtlasBuilder(32);
        var lit = b.AddLit("dot", ["#"], Token.LcdText, Token.LcdGlow);
        var mask = b.Build().Mask;

        Assert.Equal(3, lit.Halo.Width);
        Assert.Equal(3, lit.Halo.Height);

        var (cx, cy) = (lit.Halo.Source.X, lit.Halo.Source.Y);
        var centre = mask.Get(cx + 1, cy + 1).Alpha;
        var side = mask.Get(cx, cy + 1).Alpha;
        var corner = mask.Get(cx, cy).Alpha;

        Assert.True(centre > side && side > corner && corner > 0);
        Assert.Equal(Token.LcdGlow, mask.Get(cx, cy).Token);
    }

    [Fact]
    public void Blit_clips_to_the_surface_and_to_the_clip_rect()
    {
        var b = new SpriteAtlasBuilder(16);
        var sprite = b.Add("block", 4, 4, c => c.Fill(0, 0, 4, 4, Token.LcdText));
        var tinted = TintedAtlas.Bake(b.Build(), ThemePresets.BlaupunktAmber);

        var surface = new Surface(6, 6);
        surface.Blit(tinted, sprite, -2, -2);
        Assert.Equal(0xFFFFA23Au, surface[0, 0]);
        Assert.Equal(0xFFFFA23Au, surface[1, 1]);
        Assert.Equal(0u, surface[2, 2]);

        var clipped = new Surface(6, 6);
        clipped.Blit(tinted, sprite, 0, 0, new PixelRect(1, 1, 2, 2));
        Assert.Equal(0u, clipped[0, 0]);
        Assert.Equal(0xFFFFA23Au, clipped[1, 1]);
        Assert.Equal(0xFFFFA23Au, clipped[2, 2]);
        Assert.Equal(0u, clipped[3, 3]);
    }

    [Fact]
    public void Upscale_is_nearest_neighbour()
    {
        var s = new Surface(2, 1);
        s.Pixels[0] = 0xFF111111;
        s.Pixels[1] = 0xFF222222;
        var big = s.Upscale(3);

        Assert.Equal(6, big.Width);
        Assert.Equal(3, big.Height);
        Assert.Equal(0xFF111111u, big[2, 2]);
        Assert.Equal(0xFF222222u, big[3, 0]);
    }

    [Fact]
    public void Interchange_image_carries_luminance_token_and_alpha()
    {
        var mask = new MaskImage(1, 1);
        mask.Set(0, 0, Token.ButtonFace, 200, 128);
        var p = mask.ToInterchangeImage().Pixels[0];

        Assert.Equal(128u, p >> 24);
        Assert.Equal(200u, (p >> 16) & 0xFF);
        Assert.Equal((uint)Token.ButtonFace, (p >> 8) & 0xFF);
    }
}
