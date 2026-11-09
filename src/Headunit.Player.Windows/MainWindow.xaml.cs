using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Headunit.Audio;
using Headunit.Core.Graphics;
using Headunit.Core.Layout;
using Headunit.Core.Playback;
using Headunit.Core.Themes;
using Headunit.Core.Ui;
using Headunit.Skin;

namespace Headunit.Player;

public partial class MainWindow : Window
{
    private const float VolumeStep = 0.05f;

    private readonly PlayerController _player;
    private readonly MainViewState _view = new();
    private readonly MainInput _input;
    private readonly MainRenderer _renderer;
    private readonly Surface _frame = new(WindowMetrics.MainWidth, WindowMetrics.MainHeight);
    private readonly SkinSurface _surface = new(WindowMetrics.MainWidth, WindowMetrics.MainHeight);
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _frameTimer;
    private int _themeIndex;
    private bool _closed;

    public MainWindow()
    {
        InitializeComponent();

        _input = new MainInput(_view);
        _renderer = new MainRenderer(MainArt.Build(), ThemePresets.All[_themeIndex]);

        Host.Child = _surface;
        _surface.ArtPointerDown += OnArtPointerDown;
        _surface.ArtPointerMove += (_, e) => Act(_input.PointerMove(e.X, e.Y));
        _surface.ArtPointerUp += (_, e) => Act(_input.PointerUp(e.X, e.Y));
        _surface.ArtPointerCancelled += (_, _) => _input.Cancel();

        _player = new PlayerController(new NAudioEngine());
        _player.TrackFailed += OnTrackFailed;

        // ~30 fps: smooth enough for the marquee and, at M3, the visualiser;
        // a 275x116 software frame costs well under a millisecond.
        _frameTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33),
        };
        _frameTimer.Tick += (_, _) => RenderFrame();
        _frameTimer.Start();

        SourceInitialized += (_, _) =>
        {
            Host.Scale = PickInitialScale();
            RenderFrame();
        };
    }

    private TimeSpan Now => _clock.Elapsed;

    /// <summary>
    /// Open at the largest integer scale that comfortably fits the work area,
    /// so we neither open a 4x window on a small laptop panel nor a postage
    /// stamp on a 4K monitor.
    /// </summary>
    private int PickInitialScale()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var workWidth = (int)(SystemParameters.WorkArea.Width * dpi.DpiScaleX);
        var workHeight = (int)(SystemParameters.WorkArea.Height * dpi.DpiScaleY);

        // Leave room for the equaliser and library windows below the main one.
        var budgetHeight = workHeight / 3;

        return ScaledGeometry.LargestScaleThatFits(
            WindowMetrics.MainWidth, WindowMetrics.MainHeight, workWidth, budgetHeight);
    }

    // ---- Frame -------------------------------------------------------------

    private void RenderFrame()
    {
        if (_closed)
        {
            return; // e.g. the close button's own release, after the player is gone
        }

        var now = Now;
        SyncView(now);
        _renderer.Render(_frame, _view, _input, now);
        _surface.Present(_frame);
    }

    /// <summary>Copies the player's state into the plain data the renderer reads.</summary>
    private void SyncView(TimeSpan now)
    {
        _view.State = _player.State;
        _view.Position = _player.Position;
        _view.Duration = _player.Duration;
        _view.Volume = _player.Volume;
        _view.Balance = _player.Balance;
        _view.Shuffle = _player.Queue.Shuffle;
        _view.Repeat = _player.Queue.Repeat;
        _view.Info = _player.Info;

        var track = _player.Queue.Current;
        _view.SetTitle(
            track is null ? string.Empty : TitleText.Compose(_player.Queue.CurrentIndex, track, _player.Duration),
            now);
    }

    private void Say(string message) => _view.ShowMessage(message, Now);

    // ---- Pointer -----------------------------------------------------------

    private void OnArtPointerDown(object? sender, ArtPointerEventArgs e)
    {
        var action = _input.PointerDown(e.X, e.Y);
        if (action?.Kind == UiActionKind.DragWindow)
        {
            // Hand the pointer to the OS for the move; it never comes back to
            // us as a release, so drop our capture first.
            _input.Cancel();
            _surface.ReleaseMouseCapture();
            DragMove();
            return;
        }

        Act(action);
    }

    /// <summary>The one place a UI gesture becomes a player command.</summary>
    private void Act(UiAction? action)
    {
        if (action is { } a)
        {
            switch (a.Kind)
            {
                case UiActionKind.Click:
                    Click(a.Widget);
                    break;

                case UiActionKind.Volume:
                    _player.Volume = (float)a.Value;
                    break;

                case UiActionKind.Balance:
                    _player.Balance = (float)a.Value;
                    break;

                case UiActionKind.Seek:
                    _player.Seek(_player.Duration * a.Value);
                    break;
            }
        }

        // Every gesture redraws immediately; waiting for the next tick makes
        // buttons feel laggy.
        RenderFrame();
    }

    private void Click(MainWidget widget)
    {
        switch (widget)
        {
            case MainWidget.Minimize: WindowState = WindowState.Minimized; break;
            case MainWidget.Close: Close(); break;
            case MainWidget.TimeReadout: _view.ShowRemaining = !_view.ShowRemaining; break;
            case MainWidget.Previous: _player.Previous(); break;
            case MainWidget.Play: Play(); break;
            case MainWidget.Pause: _player.TogglePlayPause(); break;
            case MainWidget.Stop: _player.Stop(); break;
            case MainWidget.Next: _player.Next(); break;
            case MainWidget.Eject: OpenFiles(); break;
            case MainWidget.Shuffle: ToggleShuffle(); break;
            case MainWidget.Repeat: CycleRepeat(); break;

            // The equaliser window is M3 and the playlist window M4; until
            // then the buttons say so rather than doing nothing silently.
            case MainWidget.EqToggle: Say("EQUALIZER: NOT BUILT YET"); break;
            case MainWidget.PlaylistToggle: Say("PLAYLIST: NOT BUILT YET"); break;
        }
    }

    // ---- Transport ---------------------------------------------------------

    private void Play()
    {
        if (_player.State != PlaybackState.Playing)
        {
            _player.TogglePlayPause();
        }
    }

    private void ToggleShuffle()
    {
        _player.Queue.Shuffle = !_player.Queue.Shuffle;
        Say(TitleText.Shuffle(_player.Queue.Shuffle));
    }

    private void CycleRepeat()
    {
        _player.Queue.Repeat = _player.Queue.Repeat switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off,
        };
        Say(TitleText.Repeat(_player.Queue.Repeat));
    }

    private void NudgeVolume(float delta)
    {
        _player.Volume += delta;
        Say(TitleText.Volume(_player.Volume));
    }

    private void CycleTheme()
    {
        _themeIndex = (_themeIndex + 1) % ThemePresets.All.Count;
        _renderer.SetTheme(ThemePresets.All[_themeIndex]);
        Say($"THEME: {ThemePresets.All[_themeIndex].Name}");
    }

    private void OnTrackFailed(object? sender, PlaybackFailedEventArgs e)
        => _view.ShowMessage($"CANNOT PLAY {Path.GetFileName(e.Path)}", Now, TimeSpan.FromSeconds(4));

    // ---- Loading -----------------------------------------------------------

    private void OpenFiles()
    {
        var patterns = string.Join(';', AudioFormats.SupportedExtensions.Select(x => "*" + x));
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open audio files",
            Multiselect = true,
            Filter = $"Audio files|{patterns}|All files|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            Load(dialog.FileNames, append: false);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            // Shift-drop appends to the queue; a plain drop replaces it.
            Load(paths, append: Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
        }
    }

    private void Load(IEnumerable<string> paths, bool append)
    {
        var tracks = Expand(paths).Select(Track.FromFile).ToList();
        if (tracks.Count == 0)
        {
            Say("NO PLAYABLE FILES");
            return;
        }

        if (append && _player.Queue.Count > 0)
        {
            _player.Queue.Append(tracks);
            Say($"ADDED {tracks.Count} TRACK{(tracks.Count == 1 ? string.Empty : "S")}");
            return;
        }

        _player.Queue.Replace(tracks);
        _player.PlayAt(0);
    }

    /// <summary>Walks dropped folders, keeping files in a stable, sorted order.</summary>
    private static IEnumerable<string> Expand(IEnumerable<string> paths)
    {
        foreach (var path in paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(path))
            {
                var files = Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(AudioFormats.IsSupported)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    yield return file;
                }
            }
            else if (File.Exists(path) && AudioFormats.IsSupported(path))
            {
                yield return path;
            }
        }
    }

    // ---- Window ------------------------------------------------------------

    private void OnActivated(object? sender, EventArgs e) => _view.IsActive = true;

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _view.IsActive = false;
        _input.Cancel();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        => NudgeVolume(e.Delta > 0 ? VolumeStep : -VolumeStep);

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _frameTimer.Stop();
        _player.Dispose();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            var requested = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 1,
                Key.D2 or Key.NumPad2 => 2,
                Key.D3 or Key.NumPad3 => 3,
                Key.D4 or Key.NumPad4 => 4,
                _ => 0,
            };

            if (requested != 0)
            {
                Host.Scale = requested;
                var dpi = VisualTreeHelper.GetDpi(this);
                var physical = ScaledGeometry.ToPhysical(WindowMetrics.MainWidth, WindowMetrics.MainHeight, Host.Scale);
                Say($"ZOOM {Host.Scale}X  {physical.Width}X{physical.Height} @{dpi.DpiScaleX * 100:0}%");
                e.Handled = true;
            }

            return;
        }

        // Winamp's transport keys, muscle memory intact.
        switch (e.Key)
        {
            case Key.Z: _player.Previous(); break;
            case Key.X: Play(); break;
            case Key.C: _player.TogglePlayPause(); break;
            case Key.V: _player.Stop(); break;
            case Key.B: _player.Next(); break;
            case Key.L: OpenFiles(); break;
            case Key.S: ToggleShuffle(); break;
            case Key.R: CycleRepeat(); break;
            case Key.T: CycleTheme(); break;
            case Key.Space: _player.TogglePlayPause(); break;
            case Key.Left: _player.Seek(_player.Position - TimeSpan.FromSeconds(5)); break;
            case Key.Right: _player.Seek(_player.Position + TimeSpan.FromSeconds(5)); break;
            case Key.Up: NudgeVolume(VolumeStep); break;
            case Key.Down: NudgeVolume(-VolumeStep); break;
            default: return;
        }

        e.Handled = true;
        RenderFrame();
    }
}
