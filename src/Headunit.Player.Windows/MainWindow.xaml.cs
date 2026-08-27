using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Headunit.Core.Layout;

namespace Headunit.Player;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            Host.Scale = PickInitialScale();
            UpdateScaleReadout();
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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        var requested = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            _ => 0,
        };

        if (requested == 0)
        {
            return;
        }

        Host.Scale = requested;
        UpdateScaleReadout();
        e.Handled = true;
    }

    private void UpdateScaleReadout()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var physical = ScaledGeometry.ToPhysical(
            WindowMetrics.MainWidth, WindowMetrics.MainHeight, Host.Scale);

        ScaleReadout.Text =
            $"{Host.Scale}X  {physical.Width}x{physical.Height}PX  DPI {dpi.DpiScaleX * 100:0}%";
    }
}
