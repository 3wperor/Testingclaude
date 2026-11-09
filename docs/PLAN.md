# Retro Music Player — Project Plan

A local-file desktop music player in the Winamp 2.x form factor, with the
illumination of a 1990s car head unit: every color in the chrome is a theme
token, so the same pixel-exact UI can be amber Blaupunkt, ice-blue Alpine,
Pioneer red, or VFD green.

**Windows first (fully native .NET/WPF), macOS after (native Swift/AppKit).**

---

## 1. Decisions locked

| Question | Decision |
|---|---|
| Platform approach | Fully native per-platform. Windows ships first; macOS follows in the same visual style with some deliberately different features. |
| Windows stack | .NET 8 + WPF + NAudio |
| macOS stack (phase 3) | Swift + AppKit + AVAudioEngine |
| Visual fidelity | Pixel-exact Winamp 2 layout and form factor, drawn as our own original artwork |
| Theming | ~8 head-unit presets **plus** a full per-token color customizer; themes save/load as small JSON files |
| Sources | Local files only |
| Library | Watched folders scanned into a local SQLite index |
| Audio path | Solid mainstream: WASAPI shared mode, 32-bit float pipeline, common formats |
| v1 features | 10-band EQ + presets, spectrum visualizer, media keys + tray, theming |

### Explicitly deferred past v1
Full playlist-editor window (v1 ships a working queue, not the resizable PL
window), internet radio, podcasts, real `.wsz` Winamp skin loading, streaming
services, plugin API.

### Legal note, decided up front
We draw **all artwork ourselves**. No Winamp bitmaps, no bundled `.wsz` skins,
no "Winamp" in the product name or icon. The layout and proportions of a
utility UI aren't protected; the art and the trademark are. This also happens
to be what makes the color system possible — see §4.

---

## 2. Architecture

Three assemblies, so the engine is testable without a UI and reusable by the
Mac port's design (not its code):

```
src/
  Headunit.Core/            net8.0     — no WPF references
    Playback/            audio graph, transport, queue
    Equalizer/           10-band biquad chain + preset store
    Analysis/            FFT tap, band bucketing, peak-hold
    Library/             SQLite index, scanner, tag reading, art cache
    Themes/              theme tokens + model, presets, JSON (de)serialization
    Graphics/            mask atlas, tint LUTs, software blitter, PNG export
    Ui/                  art, fonts, layout, hit-testing, input, frame renderer
  Headunit.Audio.Windows/   net8.0-windows — WASAPI output (NAudio) behind IAudioEngine
  Headunit.Skin/            net8.0-windows — presents a frame, forwards the pointer, integer scaling
  Headunit.Player.Windows/  net8.0-windows — WPF shell, windows, input, tray, SMTC
tests/
  Headunit.Core.Tests/      xUnit — EQ math, bucketing, scanner, theme round-trip, rendering
tools/
  Headunit.SkinPreview/     renders every window in every theme to PNG
assets/
  sprites/               source PSD/Aseprite + exported grayscale PNG atlases
themes/                  built-in presets as .rtheme.json
docs/
```

**Dependencies (all MIT/Ms-PL):** NAudio (WASAPI + decode + DSP),
TagLibSharp (tags/art), Microsoft.Data.Sqlite, CommunityToolkit.Mvvm,
Microsoft.Windows.CsWinRT (for SMTC Now Playing).

### Threading model
- **Audio thread** — NAudio's WASAPI event-driven callback. Never touches UI,
  never allocates, never locks. EQ coefficients are swapped via a volatile
  reference to an immutable coefficient set.
- **UI thread** — WPF dispatcher. Reads a lock-free FFT snapshot buffer at
  frame rate; the audio thread writes it.
- **Scanner thread pool** — library scanning and art extraction, results
  batched into SQLite transactions of ~500 rows.

---

## 3. Audio engine

```
File → decoder (MediaFoundationReader / NAudio.Vorbis / FLAC)
     → resample to device rate (MediaFoundationResampler)
     → PreampProvider          (±12 dB)
     → EqualizerProvider       (10 × BiquadFilter, per channel)
     → SoftClipProvider        (prevents EQ boost clipping)
     → VolumeProvider          (with a smoothing ramp — no zipper noise)
     → FftTapProvider          (passthrough; copies to analysis ring buffer)
     → WasapiOut (shared, event-driven, ~30 ms latency)
```

**EQ bands** — the classic ten: 60, 170, 310, 600, 1k, 3k, 6k, 12k, 14k, 16k Hz.
Peaking filters, ±12 dB, Q ≈ 1.0. Preamp is a separate ±12 dB gain stage. On /
Auto / Presets, matching the screenshot's controls; "Auto" applies a saved
per-track or per-genre preset on load.

Preset format is our own JSON, but the importer accepts Winamp's `winamp.q1`
binary layout (each band 0–63, 31 = flat) so existing preset packs are usable.

**Formats:** MP3, AAC/M4A, WAV, WMA via Media Foundation; FLAC and OGG via
NAudio codecs. Anything unsupported fails to a visible error in the LCD readout,
never a silent skip.

**Crossfade** between tracks (0–10 s, configurable, default 0) is v1;
sample-accurate gapless is phase 2 with the audiophile path.

### Visualizer
1024-point FFT with a Hann window at ~30 Hz update. Magnitudes are bucketed
into 19 logarithmically-spaced bars to match the classic display width, with
per-bar peak-hold caps that fall at a fixed rate. Modes: spectrum bars,
oscilloscope, off — cycled by clicking the display, exactly as the original.
Rendered to a `WriteableBitmap` (the panel is tiny; this costs almost nothing).

---

## 4. The skin + color system

This is the part that makes the whole idea work, so it's worth being precise.

Classic Winamp baked colors into the bitmaps, which is why changing a skin's
color meant re-authoring every BMP. We invert that:

**Every sprite is authored once, in grayscale + alpha, as a luminance mask.**
At runtime a theme maps each sprite region to a color *ramp* (shadow →
midtone → highlight), and we bake the tinted atlas into a cached bitmap
**once per theme change** — not per frame. Drawing is then plain sprite blits,
as cheap as the original.

```
tinted.rgb = lut[ source.token ][ source.luminance ]   // 256-entry LUT per token
tinted.a   = source.a × (glow strength, for lcd.glow)
```

Each mask pixel carries its **token** as well as its luminance, so one sprite
can mix bezel, glyph and LCD pixels. A ramp token runs shadow (0) → mid (128)
→ high (255); a flat token runs black (0) → its colour (255), so art drawn at
full luminance lands exactly on the theme author's hex value. The interchange
PNG stores luminance in R, token index in G and alpha in A.

LCD elements are baked twice — a lit core and a soft one-pixel halo in
`lcd.glow` — and text draws every halo before any core so neighbouring glyphs'
glow never washes over each other. Unlit seven-segment and indicator segments
are printed into the background in `lcd.text.dim` ("ghost segments"), which is
why that token wants to be *much* darker than `lcd.text`.

As built at M2: a 512×215 atlas of 219 sprites, 0.7 ms to re-tint, ~0.15 ms to
render a full frame.

That single decision gives us: infinite colorways, one set of source art, no
shader requirement, and pixel-exact 1x output.

### Theme tokens
```jsonc
{
  "name": "Blaupunkt Amber",
  "version": 1,
  "tokens": {
    "bezel.face":        { "shadow": "#2A2A2C", "mid": "#4A4A4E", "high": "#75757A" },
    "bezel.edge.light":  "#8E8E94",
    "bezel.edge.dark":   "#141416",
    "lcd.background":    "#120A00",
    "lcd.text":          "#FFA23A",
    "lcd.text.dim":      "#7A4A12",
    "lcd.glow":          { "color": "#FF8A00", "strength": 0.35 },
    "button.face":       { "shadow": "#333336", "mid": "#54545A", "high": "#7E7E86" },
    "button.glyph":      "#E8E8EC",
    "button.active":     "#FFA23A",
    "slider.track":      "#1A1A1D",
    "slider.thumb":      { "shadow": "#4A4A4E", "mid": "#8A8A92", "high": "#C6C6CE" },
    "spectrum.low":      "#FF6A00",
    "spectrum.high":     "#FFD98A",
    "spectrum.peak":     "#FFFFFF",
    "titlebar.active":   "#3A3A3E",
    "titlebar.inactive": "#242427"
  }
}
```

**Built-in presets:** Blaupunkt Amber · Alpine Ice · Pioneer Red · VFD Green ·
Nakamichi Gold · Becker Teal · Sony Xplod Violet · Classic Grey (a neutral,
Winamp-ish default).

**Customizer window** — a fourth window in the stack, same chrome as the rest:
token list on the left, live preview of the main window on the right, a color
picker with eyedropper, "derive ramp from one color" (so a user picks *one*
illumination color and shadow/mid/high are generated), plus Save As / Export /
Import. Themes live in `%APPDATA%/Headunit/themes/*.rtheme.json` and are
single files, so they're trivially shareable.

### Window model
- Borderless `WindowChrome` windows: Main (275×116), Equalizer (275×116),
  Library (resizable, 275 wide minimum).
- **Integer scaling only** — 1x/2x/3x/4x with `NearestNeighbor` bitmap scaling,
  per-monitor DPI v2 aware. No fractional scaling, ever; it would smear the art.
- **Magnetic docking**: windows snap edge-to-edge within 10 px and then move as
  a group when the main window is dragged. This is what makes it *feel* like
  Winamp more than any single sprite does.
- **Shade mode**: main window collapses to a 275×14 title strip with a mini
  time readout and scrolling title.
- Always-on-top toggle, remembered window positions per monitor layout.

---

## 5. Library

- **Watched folders**, added via a settings pane. `FileSystemWatcher` plus a
  full rescan on launch (debounced, so a folder copy doesn't thrash).
- **SQLite** with FTS5 for search:
  `tracks(id, path, title, artist, album, albumartist, track_no, disc_no,
  year, genre, duration_ms, bitrate, samplerate, channels, codec, size,
  mtime, added_utc, art_id, play_count, last_played_utc)` plus
  `albums`, `artists`, `playlists`, `playlist_items`, `tracks_fts`.
- **Identity is `path`, keyed to `(size, mtime)`** for change detection — no
  rehashing of files that haven't moved.
- **Album art** extracted to `%LOCALAPPDATA%/Headunit/artcache/{hash}.jpg`,
  deduplicated by content hash; `folder.jpg`/`cover.png` used as fallback.
- Playlists stored in the DB and exportable as `.m3u8`.

v1 surfaces the library as: search box, artist/album/track list, drag to queue,
double-click to play. The full Winamp-style PL window comes in phase 2.

---

## 6. OS integration (Windows)

- **Media keys** via `RegisterHotKey` on `VK_MEDIA_*` — with a fallback to a
  low-level keyboard hook only if registration is refused by another app.
- **SMTC** (`SystemMediaTransportControls`) so the Windows Now Playing flyout,
  Bluetooth headsets, and car units show title/artist/art and respond to
  transport controls.
- **Tray icon** with a mini transport menu; close-to-tray optional.
- **File associations** for audio types, plus "Play with", "Enqueue in
  Headunit" shell verbs, and command-line `--enqueue` for single-instance
  handoff via a named pipe.
- **Single instance** — second launch forwards its file arguments and exits.
- **Settings** in `%APPDATA%/Headunit/settings.json`; the app is
  fully portable if a `portable.txt` sits next to the exe.

---

## 7. Milestones

| # | Milestone | What "done" means |
|---|---|---|
| **M0** | Skeleton | ✅ Solution, projects, CI build, xUnit runs. Borderless 275×116 window at 1x–4x. |
| **M1** | Audio spine | ✅ Play/pause/stop/seek/next/prev through WASAPI, drag-and-drop loading, shuffle/repeat, correct time readout. No skin yet. |
| **M2** | Skin engine | ✅ Grayscale atlas + tint LUT + sprite blitting. Main window fully drawn and interactive: transport, seek bar, volume, balance, LCD readout with scrolling title. |
| **M3** | EQ + visualizer | Equalizer window with working DSP, preamp, on/auto, preset save/load/import. Spectrum + oscilloscope modes. |
| **M4** | Library | Watched folders, scanner, SQLite + FTS5, art cache, library window, queue, shuffle/repeat. |
| **M5** | Theming | Theme model, 8 presets, customizer window, import/export, live re-tint. |
| **M6** | Ship v1 (Windows) | Media keys, SMTC, tray, file associations, single instance, settings, crash logging, signed MSI + auto-update check. |
| **M7** | macOS | Swift/AppKit port — see §8. |

M1–M3 is the fun part and the riskiest; do them in order and resist starting
the library early.

---

## 8. macOS phase — same style, different features

The Mac build re-implements the UI natively rather than sharing code, but it
**shares the theme JSON files and the sprite atlases byte-for-byte**, so a theme
made on Windows looks identical on Mac.

- **Audio**: `AVAudioEngine` → `AVAudioUnitEQ` (10 bands, free) →
  `installTap` for FFT via Accelerate/vDSP → `AVAudioPlayerNode`.
- **UI**: borderless `NSWindow` with a custom `NSView` drawing the same atlas;
  window snapping via `NSWindow` child-window groups.
- **Different by design on Mac:** menubar (`NSStatusItem`) player instead of a
  tray, native Now Playing via `MPNowPlayingInfoCenter`, "Add to Library" via
  Finder services, Spotlight-indexed library, and a Quick Look-style large art
  view. Open question — see §10 — is which of these you actually want.

---

## 9. Risks

| Risk | Mitigation |
|---|---|
| Pixel-exact art is a genuine time sink | Build the atlas at 1x in Aseprite with a strict 4-color-ramp discipline; one widget style sheet reused across all windows. Budget real time for M2. |
| WPF + per-monitor DPI + integer scaling can fight each other | Set `PerMonitorV2` in the manifest, render at 1x into a `WriteableBitmap`, scale the whole surface once with NearestNeighbor. Solve this at M0, not M5. |
| Media key registration conflicts with Spotify/browsers | Try `RegisterHotKey`; on failure, fall back to a hook and show a settings warning rather than failing silently. |
| Library scan of a 100k-track folder blocking the UI | Background scan, batched transactions, progress in the title bar; playback never waits on the scanner. |
| Trademark exposure | Original art, original name, no bundled Winamp assets. Decided in §1. |
| Scope creep toward skins/streaming/plugins | Everything in "Explicitly deferred" stays deferred until v1 Windows ships. |

---

## 10. Decisions taken since planning

| Was open | Decided | Why |
|---|---|---|
| Name | **Headunit** | Says what it is, distinctive, no Winamp trademark exposure. Namespaces `Headunit.Core` / `Headunit.Skin` / `Headunit.Player.Windows`; the executable is `Headunit.exe`. |
| Library window chrome in v1 | **Plain modern list** | The M2 art budget is the largest schedule risk in this plan. Doubling it to chrome a window that phase 2 replaces with the real PL window is the wrong trade. |
| Auto-update | **GitHub Releases + check on launch** | A version check and a "download" link. No silent background installer — this is a music player, not a browser. |
| Code signing | **Ship v1 unsigned** | ~$200/yr for an OV cert is not worth it before anyone has used the thing. Revisit at the first public release; document the SmartScreen warning in the README until then. |
| Minimum Windows | **Windows 10 1809 (build 17763), x64 + arm64** | The floor for `SystemMediaTransportControls` and CsWinRT. Encoded in the TFM as `net8.0-windows10.0.17763.0`.|
| Where the skin engine lives | **`Headunit.Core`, not `Headunit.Skin`** | The whole pixel pipeline — atlas, tinting, layout, hit-testing, the frame renderer — is pure C# over a `uint[]`. Putting it in Core means it is unit-tested on Linux CI and renders identically everywhere; `Headunit.Skin` shrinks to "copy frame into a `WriteableBitmap`, report the pointer in art pixels". |
| How the art is authored | **In code (`Ui/MainArt.cs`), exportable as PNG** | Diffable, reviewable, exactly reproducible, and no binary art files to merge. `SkinPreview` exports the mask atlas, so hand-drawn art can replace any sprite later without touching the renderer. |
| LCD character set | **Uppercase 5×7 dot matrix, with Latin folding** | What real head units show. Accented Latin folds to its base letter (BJÖRK → BJORK); scripts the font can't draw show as `?` until a fallback font is worth the art budget. |
| Clock past 99 minutes | **Switches to H:MM** | Four digits can't show 1:45:00 as MM:SS; a long DJ mix should read as hours, not lie. |
| Repeat-one | **"RPT 1" on the button and an extra `1` segment on the LCD** | The classic layout has one repeat toggle; we have three states and needed to show the third without a new control. |

## 11. Still open

1. **Which Mac-specific features from §8 do you actually want**, and are any
   worth backporting to Windows? Not blocking until M7.
2. **Crossfade default** — 0 s (true to the original) or 2 s (nicer for most
   listeners)? Decide when M1 lands and it can actually be heard.
3. **Bundled themes beyond the eight presets** — worth shipping a theme
   gallery/import UI in v1, or is file drag-and-drop enough?
