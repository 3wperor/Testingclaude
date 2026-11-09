using Headunit.Core.Graphics;
using Headunit.Core.Layout;
using Headunit.Core.Playback;
using Headunit.Core.Themes;
using Headunit.Core.Ui;

// Renders the main window, exactly as the app draws it, to PNG — one per
// built-in theme, plus a contact sheet and the raw atlas mask. Runs anywhere
// .NET does, so the skin can be reviewed without a Windows machine.
//
//   dotnet run --project tools/Headunit.SkinPreview -- [output-dir] [scale]

var output = args.Length > 0 ? args[0] : "skin-preview";
var scale = args.Length > 1 ? int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 2;
Directory.CreateDirectory(output);

var art = MainArt.Build();
var renderer = new MainRenderer(art, ThemePresets.Default);
var frame = new Surface(WindowMetrics.MainWidth, WindowMetrics.MainHeight);

var view = new MainViewState
{
    State = PlaybackState.Playing,
    Position = TimeSpan.FromSeconds(83),
    Duration = TimeSpan.FromSeconds(332),
    Volume = 0.72f,
    Balance = 0f,
    Shuffle = true,
    Repeat = RepeatMode.All,
    Info = new StreamInfo(44100, 2, 192),
};
view.SetTitle("3. Night Drive - Autobahn Lights (5:32)", TimeSpan.Zero);
var input = new MainInput(view);

var sheet = new Surface(WindowMetrics.MainWidth, (WindowMetrics.MainHeight + 4) * ThemePresets.All.Count - 4);
sheet.Clear(0x00000000);

for (var i = 0; i < ThemePresets.All.Count; i++)
{
    var theme = ThemePresets.All[i];
    renderer.SetTheme(theme);
    renderer.Render(frame, view, input, TimeSpan.Zero);

    var slug = theme.Name.ToLowerInvariant().Replace(' ', '-');
    PngEncoder.Write(frame.Upscale(scale), Path.Combine(output, $"main-{slug}.png"));
    Array.Copy(frame.Pixels, 0, sheet.Pixels, i * (WindowMetrics.MainHeight + 4) * WindowMetrics.MainWidth, frame.Pixels.Length);
}

PngEncoder.Write(sheet.Upscale(scale), Path.Combine(output, "themes.png"));
PngEncoder.Write(art.Atlas.Mask.ToInterchangeImage(), Path.Combine(output, "atlas-mask.png"));

// The same window mid-interaction: dragging volume, paused, no shuffle.
renderer.SetTheme(ThemePresets.Default);
view.State = PlaybackState.Paused;
view.Shuffle = false;
view.Repeat = RepeatMode.One;
view.ShowRemaining = true;
input.PointerDown(MainLayout.Play.X + 3, MainLayout.Play.Y + 3);
renderer.Render(frame, view, input, TimeSpan.Zero);
PngEncoder.Write(frame.Upscale(scale), Path.Combine(output, "main-interaction.png"));

input.Cancel();
input.PointerDown(MainLayout.VolumeSlider.ThumbX(view.Volume) + 2, MainLayout.Volume.Y + 5);
input.PointerMove(MainLayout.Volume.X + 20, MainLayout.Volume.Y + 5);
renderer.Render(frame, view, input, TimeSpan.Zero);
PngEncoder.Write(frame.Upscale(scale), Path.Combine(output, "main-volume-drag.png"));

var empty = new MainViewState();
renderer.Render(frame, empty, new MainInput(empty), TimeSpan.Zero);
PngEncoder.Write(frame.Upscale(scale), Path.Combine(output, "main-empty.png"));

Console.WriteLine($"Wrote previews at {scale}x to {Path.GetFullPath(output)}");
