using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Headunit.Core.Graphics;

namespace Headunit.Skin;

/// <summary>Pointer position in art pixels — the same coordinates as the layout.</summary>
public sealed class ArtPointerEventArgs(int x, int y) : EventArgs
{
    public int X { get; } = x;

    public int Y { get; } = y;
}

/// <summary>
/// Presents a software-rendered frame and reports the pointer in art pixels.
/// <para>
/// This element is deliberately dumb: it is exactly one <see cref="WriteableBitmap"/>
/// at 1:1 art pixels. All drawing happens in <c>Headunit.Core</c>; all scaling
/// happens once, in <see cref="ScaledHost"/>, with nearest-neighbour. That split
/// is what keeps every art pixel a whole number of screen pixels.
/// </para>
/// </summary>
public sealed class SkinSurface : FrameworkElement
{
    private readonly WriteableBitmap _bitmap;
    private readonly Int32Rect _dirty;

    public SkinSurface(int width, int height)
    {
        PixelWidth = width;
        PixelHeight = height;
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        _dirty = new Int32Rect(0, 0, width, height);

        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        UseLayoutRounding = false;
        SnapsToDevicePixels = false;
        Focusable = false;
    }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public event EventHandler<ArtPointerEventArgs>? ArtPointerDown;

    public event EventHandler<ArtPointerEventArgs>? ArtPointerMove;

    public event EventHandler<ArtPointerEventArgs>? ArtPointerUp;

    /// <summary>The pointer was taken away mid-gesture (another window, a window drag); drop it.</summary>
    public event EventHandler? ArtPointerCancelled;

    /// <summary>Copies a finished frame to the screen. 0xAARRGGBB is Bgra32 in memory.</summary>
    public void Present(Surface frame)
    {
        if (frame.Width != PixelWidth || frame.Height != PixelHeight)
        {
            throw new ArgumentException("Frame size does not match the surface.", nameof(frame));
        }

        _bitmap.WritePixels(_dirty, frame.Pixels, PixelWidth * 4, 0);
    }

    protected override Size MeasureOverride(Size availableSize) => new(PixelWidth, PixelHeight);

    protected override Size ArrangeOverride(Size finalSize) => new(PixelWidth, PixelHeight);

    protected override void OnRender(DrawingContext drawingContext)
        => drawingContext.DrawImage(_bitmap, new Rect(0, 0, PixelWidth, PixelHeight));

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        // Capture so a slider keeps tracking — and a button can see the
        // release — even when the pointer leaves the window mid-drag.
        CaptureMouse();
        ArtPointerDown?.Invoke(this, ToArt(e));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsMouseCaptured)
        {
            ArtPointerMove?.Invoke(this, ToArt(e));
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (IsMouseCaptured)
        {
            // Report the release before letting go: releasing raises
            // LostMouseCapture, which would otherwise cancel the click.
            ArtPointerUp?.Invoke(this, ToArt(e));
            ReleaseMouseCapture();
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        ArtPointerCancelled?.Invoke(this, EventArgs.Empty);
    }

    private ArtPointerEventArgs ToArt(MouseEventArgs e)
    {
        // GetPosition is in this element's own coordinates, which are art
        // pixels: the integer zoom is a LayoutTransform on the host above us.
        var p = e.GetPosition(this);
        return new ArtPointerEventArgs((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
    }
}
