using Headunit.Core.Graphics;

namespace Headunit.Core.Themes;

/// <summary>
/// Built-in illuminations. M2 ships four, enough to prove the re-tint; the rest
/// of the eight, the JSON files and the customiser are M5.
/// </summary>
public static class ThemePresets
{
    public static Theme BlaupunktAmber { get; } = Build("Blaupunkt Amber", new()
    {
        [Token.BezelFace] = Ramp("#2A2A2C", "#4A4A4E", "#75757A"),
        [Token.BezelEdgeLight] = Flat("#8E8E94"),
        [Token.BezelEdgeDark] = Flat("#141416"),
        [Token.LcdBackground] = Flat("#120A00"),
        [Token.LcdText] = Flat("#FFA23A"),
        [Token.LcdTextDim] = Flat("#2E1C06"),
        [Token.LcdGlow] = Glow("#FF8A00", 0.35),
        [Token.ButtonFace] = Ramp("#333336", "#54545A", "#7E7E86"),
        [Token.ButtonGlyph] = Flat("#E8E8EC"),
        [Token.ButtonActive] = Flat("#FFA23A"),
        [Token.SliderTrack] = Flat("#1A1A1D"),
        [Token.SliderThumb] = Ramp("#4A4A4E", "#8A8A92", "#C6C6CE"),
        [Token.SpectrumLow] = Flat("#FF6A00"),
        [Token.SpectrumHigh] = Flat("#FFD98A"),
        [Token.SpectrumPeak] = Flat("#FFFFFF"),
        [Token.TitlebarActive] = Flat("#4A4A50"),
        [Token.TitlebarInactive] = Flat("#2C2C30"),
    });

    public static Theme AlpineIce { get; } = Build("Alpine Ice", new()
    {
        [Token.BezelFace] = Ramp("#1E2024", "#3C4048", "#6A7180"),
        [Token.BezelEdgeLight] = Flat("#8C94A0"),
        [Token.BezelEdgeDark] = Flat("#0E1013"),
        [Token.LcdBackground] = Flat("#00060E"),
        [Token.LcdText] = Flat("#8FD8FF"),
        [Token.LcdTextDim] = Flat("#0E2638"),
        [Token.LcdGlow] = Glow("#3AB4FF", 0.4),
        [Token.ButtonFace] = Ramp("#2A2D33", "#4C5059", "#7A808C"),
        [Token.ButtonGlyph] = Flat("#E6F2FF"),
        [Token.ButtonActive] = Flat("#8FD8FF"),
        [Token.SliderTrack] = Flat("#12151A"),
        [Token.SliderThumb] = Ramp("#4A4F58", "#8A919E", "#C8D0DC"),
        [Token.SpectrumLow] = Flat("#2A8CFF"),
        [Token.SpectrumHigh] = Flat("#BFF0FF"),
        [Token.SpectrumPeak] = Flat("#FFFFFF"),
        [Token.TitlebarActive] = Flat("#454A54"),
        [Token.TitlebarInactive] = Flat("#272A30"),
    });

    public static Theme PioneerRed { get; } = Build("Pioneer Red", new()
    {
        [Token.BezelFace] = Ramp("#1C1C1E", "#38383C", "#626268"),
        [Token.BezelEdgeLight] = Flat("#7E7E86"),
        [Token.BezelEdgeDark] = Flat("#0C0C0D"),
        [Token.LcdBackground] = Flat("#120000"),
        [Token.LcdText] = Flat("#FF3B30"),
        [Token.LcdTextDim] = Flat("#2E0805"),
        [Token.LcdGlow] = Glow("#FF1A00", 0.4),
        [Token.ButtonFace] = Ramp("#2C2C2F", "#4A4A4F", "#76767C"),
        [Token.ButtonGlyph] = Flat("#F0E6E6"),
        [Token.ButtonActive] = Flat("#FF3B30"),
        [Token.SliderTrack] = Flat("#151516"),
        [Token.SliderThumb] = Ramp("#46464B", "#86868E", "#C2C2CA"),
        [Token.SpectrumLow] = Flat("#FF2200"),
        [Token.SpectrumHigh] = Flat("#FF9C8A"),
        [Token.SpectrumPeak] = Flat("#FFFFFF"),
        [Token.TitlebarActive] = Flat("#424247"),
        [Token.TitlebarInactive] = Flat("#252528"),
    });

    public static Theme VfdGreen { get; } = Build("VFD Green", new()
    {
        [Token.BezelFace] = Ramp("#18181A", "#303033", "#56565B"),
        [Token.BezelEdgeLight] = Flat("#76767E"),
        [Token.BezelEdgeDark] = Flat("#08080A"),
        [Token.LcdBackground] = Flat("#000A06"),
        [Token.LcdText] = Flat("#4DFFC3"),
        [Token.LcdTextDim] = Flat("#0A3326"),
        [Token.LcdGlow] = Glow("#00FFAA", 0.45),
        [Token.ButtonFace] = Ramp("#29292C", "#46464A", "#707076"),
        [Token.ButtonGlyph] = Flat("#DDF5EC"),
        [Token.ButtonActive] = Flat("#4DFFC3"),
        [Token.SliderTrack] = Flat("#111113"),
        [Token.SliderThumb] = Ramp("#424246", "#82828A", "#BEBEC6"),
        [Token.SpectrumLow] = Flat("#00D98C"),
        [Token.SpectrumHigh] = Flat("#B8FFE6"),
        [Token.SpectrumPeak] = Flat("#FFFFFF"),
        [Token.TitlebarActive] = Flat("#3E3E43"),
        [Token.TitlebarInactive] = Flat("#222225"),
    });

    public static IReadOnlyList<Theme> All { get; } = [BlaupunktAmber, AlpineIce, PioneerRed, VfdGreen];

    public static Theme Default => BlaupunktAmber;

    private static Theme Build(string name, Dictionary<Token, TokenValue> values) => new(name, values);

    private static FlatToken Flat(string hex) => new(Rgb.Parse(hex));

    private static RampToken Ramp(string shadow, string mid, string high)
        => new(Rgb.Parse(shadow), Rgb.Parse(mid), Rgb.Parse(high));

    private static GlowToken Glow(string hex, double strength) => new(Rgb.Parse(hex), strength);
}
