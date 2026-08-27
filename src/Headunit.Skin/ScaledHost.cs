using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Headunit.Core.Layout;

namespace Headunit.Skin;

/// <summary>
/// Hosts the 1:1 art-pixel UI and blows it up to an integer scale without
/// resampling.
/// <para>
/// Two things have to be true at once: the child lays out in art pixels
/// (so every sprite coordinate in the codebase is the number in the atlas),
/// and one art pixel lands on exactly N whole physical pixels (so nearest-
/// neighbour stays crisp). <see cref="ScaledGeometry.LayoutScale"/> reconciles
/// them by pre-dividing out the monitor's DPI scale.
/// </para>
/// </summary>
public class ScaledHost : Decorator
{
    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(
            nameof(Scale),
            typeof(int),
            typeof(ScaledHost),
            new FrameworkPropertyMetadata(
                1,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnScaleChanged,
                CoerceScale));

    /// <summary>Integer zoom, 1x to 4x. Values outside that range are coerced.</summary>
    public int Scale
    {
        get => (int)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public ScaledHost()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        // Layout rounding would snap our deliberately-fractional DIP sizes to
        // whole units, which is exactly the smearing we are avoiding.
        UseLayoutRounding = false;
        SnapsToDevicePixels = false;

        Loaded += (_, _) => ApplyTransform();
        ApplyTransform();
    }

    private static object CoerceScale(DependencyObject d, object baseValue)
        => ScaledGeometry.ClampScale((int)baseValue);

    private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ScaledHost)d).ApplyTransform();

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        // Before the visual is connected this reports the system DPI, which is
        // a fine first guess; OnDpiChanged corrects it once we have an HWND.
        var dpi = VisualTreeHelper.GetDpi(this);
        var factor = ScaledGeometry.LayoutScale(Scale, dpi.DpiScaleX);
        LayoutTransform = new ScaleTransform(factor, factor);
    }
}
