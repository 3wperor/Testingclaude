# Headunit

A local-file desktop music player in the Winamp 2.x form factor, lit like a
1990s car head unit — every colour in the chrome is a theme token, so the same
pixel-exact UI can be amber Blaupunkt, ice-blue Alpine, Pioneer red, or VFD
green.

**Status: M1 — audio spine.** It plays music. Drag files or a folder onto the
window and it works as a transport: play, pause, stop, seek, next, previous,
shuffle, repeat. No skin yet — the sprite atlas is M2.

- **Windows first** — .NET 8 + WPF + NAudio, fully native, Windows 10 1809+.
- **macOS after** — Swift + AppKit + AVAudioEngine, same visual style, some
  deliberately different features.
- **v1 scope** — local files, 10-band EQ with presets, spectrum visualiser,
  media keys + tray, and the theme customiser.

👉 **[Read the full plan in `docs/PLAN.md`](docs/PLAN.md)**

## Layout

| Project | Target | What it is |
|---|---|---|
| `src/Headunit.Core` | `net8.0` | Playback, EQ, analysis, library, themes. No Windows dependencies, so it is testable anywhere. |
| `src/Headunit.Audio.Windows` | `net8.0-windows10.0.17763.0` | WASAPI output via NAudio, behind `IAudioEngine`. |
| `src/Headunit.Skin` | `net8.0-windows10.0.17763.0` | Sprite atlas, runtime tinting, and the integer-scaling host. |
| `src/Headunit.Player.Windows` | `net8.0-windows10.0.17763.0` | The WPF shell. Builds `Headunit.exe`. |
| `tests/Headunit.Core.Tests` | `net8.0` | xUnit tests for the engine. |

## Building

```bash
dotnet build Headunit.sln
dotnet test  tests/Headunit.Core.Tests/Headunit.Core.Tests.csproj
```

On Linux and macOS the Windows projects still **compile** (the build sets
`EnableWindowsTargeting` for you) so you get real feedback on non-Windows
machines — but only Windows can run the app.

## Running

```bash
dotnet run --project src/Headunit.Player.Windows
```

Drop audio files or a folder onto the window to load a queue; hold **Shift**
while dropping to append instead of replace.

| Key | Does |
|---|---|
| `Z` `X` `C` `V` `B` | Previous, play, pause, stop, next — Winamp's own layout |
| `Space` | Play / pause |
| `←` `→` | Seek 5 seconds |
| `↑` `↓` | Volume |
| `S` / `R` | Shuffle / cycle repeat off → all → one |
| `Ctrl+1` … `Ctrl+4` | Zoom 1x to 4x |

Click the time readout to toggle elapsed and remaining. Drag the title strip to
move the window.

---

All artwork is drawn from scratch. This project bundles no Winamp assets and is
not affiliated with Winamp.
