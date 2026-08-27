# Headunit

A local-file desktop music player in the Winamp 2.x form factor, lit like a
1990s car head unit — every colour in the chrome is a theme token, so the same
pixel-exact UI can be amber Blaupunkt, ice-blue Alpine, Pioneer red, or VFD
green.

**Status: M0 — skeleton.** The solution builds, the scaling model works, and
there is a borderless 275×116 window. No audio yet.

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

`Ctrl+1` … `Ctrl+4` switch between 1x and 4x. Drag the title strip to move the
window; the readout shows the current scale, the exact physical pixel size, and
the monitor's DPI scale.

---

All artwork is drawn from scratch. This project bundles no Winamp assets and is
not affiliated with Winamp.
