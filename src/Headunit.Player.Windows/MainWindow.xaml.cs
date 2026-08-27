using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Headunit.Audio;
using Headunit.Core.Layout;
using Headunit.Core.Playback;

namespace Headunit.Player;

public partial class MainWindow : Window
{
    private readonly PlayerController _player;
    private readonly DispatcherTimer _tick;
    private bool _showRemaining;
    private bool _updatingSeekBar;

    public MainWindow()
    {
        InitializeComponent();

        _player = new PlayerController(new NAudioEngine());
        _player.TrackChanged += (_, _) => Refresh();
        _player.StateChanged += (_, _) => Refresh();
        _player.TrackFailed += OnTrackFailed;

        // 100 ms is comfortably below the eye's tolerance for a ticking clock
        // and nowhere near often enough to matter for CPU.
        _tick = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _tick.Tick += (_, _) => Refresh();
        _tick.Start();

        SourceInitialized += (_, _) =>
        {
            Host.Scale = PickInitialScale();
            Refresh();
        };
    }

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

    private void Refresh()
    {
        var duration = _player.Duration;
        var position = _player.Position;

        TimeReadout.Text = TimeDisplay.Format(position, duration, _showRemaining);
        StateReadout.Text = _player.State.ToString().ToUpperInvariant();

        var track = _player.Queue.Current;
        if (track is not null)
        {
            var ordinal = _player.Queue.CurrentIndex + 1;
            TitleReadout.Text = $"{ordinal}. {track.Title}".ToUpperInvariant();
        }

        _updatingSeekBar = true;
        SeekBar.Value = duration > TimeSpan.Zero
            ? Math.Clamp(position.TotalSeconds / duration.TotalSeconds, 0, 1)
            : 0;
        _updatingSeekBar = false;

        ShuffleButton.Opacity = _player.Queue.Shuffle ? 1.0 : 0.45;
        RepeatButton.Opacity = _player.Queue.Repeat == RepeatMode.Off ? 0.45 : 1.0;
        RepeatButton.Content = _player.Queue.Repeat == RepeatMode.One ? "RPT1" : "RPT";

        UpdateScaleReadout();
    }

    private void UpdateScaleReadout()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var physical = ScaledGeometry.ToPhysical(
            WindowMetrics.MainWidth, WindowMetrics.MainHeight, Host.Scale);

        ScaleReadout.Text = $"{Host.Scale}X {physical.Width}x{physical.Height} @{dpi.DpiScaleX * 100:0}%";
    }

    private void OnTrackFailed(object? sender, PlaybackFailedEventArgs e)
        => TitleReadout.Text = $"CANNOT PLAY {Path.GetFileName(e.Path)}".ToUpperInvariant();

    // ---- Loading -----------------------------------------------------------

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        var tracks = Expand(paths).Select(Track.FromFile).ToList();
        if (tracks.Count == 0)
        {
            TitleReadout.Text = "NO PLAYABLE FILES";
            return;
        }

        // Shift-drop appends to the queue; a plain drop replaces it.
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && _player.Queue.Count > 0)
        {
            _player.Queue.Append(tracks);
            Refresh();
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

    // ---- Transport ---------------------------------------------------------

    private void OnPreviousClick(object sender, RoutedEventArgs e) => _player.Previous();

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        if (_player.State != PlaybackState.Playing)
        {
            _player.TogglePlayPause();
        }
    }

    private void OnPauseClick(object sender, RoutedEventArgs e) => _player.TogglePlayPause();

    private void OnStopClick(object sender, RoutedEventArgs e) => _player.Stop();

    private void OnNextClick(object sender, RoutedEventArgs e) => _player.Next();

    private void OnShuffleClick(object sender, RoutedEventArgs e)
    {
        _player.Queue.Shuffle = !_player.Queue.Shuffle;
        Refresh();
    }

    private void OnRepeatClick(object sender, RoutedEventArgs e)
    {
        _player.Queue.Repeat = _player.Queue.Repeat switch
        {
            RepeatMode.Off => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            _ => RepeatMode.Off,
        };
        Refresh();
    }

    private void OnSeekBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // The timer writes this control every 100 ms; only react to the user.
        if (_updatingSeekBar || _player.Duration <= TimeSpan.Zero)
        {
            return;
        }

        _player.Seek(_player.Duration * e.NewValue);
    }

    private void OnTimeReadoutClick(object sender, MouseButtonEventArgs e)
    {
        _showRemaining = !_showRemaining;
        Refresh();
    }

    // ---- Window ------------------------------------------------------------

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _tick.Stop();
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
                Refresh();
                e.Handled = true;
            }

            return;
        }

        // Winamp's transport keys, muscle memory intact.
        switch (e.Key)
        {
            case Key.Z: _player.Previous(); break;
            case Key.X: OnPlayClick(sender, new RoutedEventArgs()); break;
            case Key.C: _player.TogglePlayPause(); break;
            case Key.V: _player.Stop(); break;
            case Key.B: _player.Next(); break;
            case Key.S: OnShuffleClick(sender, new RoutedEventArgs()); break;
            case Key.R: OnRepeatClick(sender, new RoutedEventArgs()); break;
            case Key.Space: _player.TogglePlayPause(); break;
            case Key.Left: _player.Seek(_player.Position - TimeSpan.FromSeconds(5)); break;
            case Key.Right: _player.Seek(_player.Position + TimeSpan.FromSeconds(5)); break;
            case Key.Up: _player.Volume += 0.05f; break;
            case Key.Down: _player.Volume -= 0.05f; break;
            default: return;
        }

        e.Handled = true;
    }
}
