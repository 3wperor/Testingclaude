namespace Headunit.Core.Ui;

public enum UiActionKind
{
    /// <summary>A button was pressed and released over itself, or a readout was clicked.</summary>
    Click,

    /// <summary>The title bar was grabbed; the shell should start a window drag.</summary>
    DragWindow,

    /// <summary>Volume moved (live, while dragging). <see cref="UiAction.Value"/> is 0..1.</summary>
    Volume,

    /// <summary>Balance moved (live, while dragging). <see cref="UiAction.Value"/> is -1..1.</summary>
    Balance,

    /// <summary>The seek thumb was released. <see cref="UiAction.Value"/> is 0..1 of the track.</summary>
    Seek,
}

public readonly record struct UiAction(UiActionKind Kind, MainWidget Widget, double Value = 0);

/// <summary>
/// Pointer handling for the main window, in art pixels.
/// <para>
/// Buttons behave like real ones: they act on <em>release</em>, and only if the
/// pointer is still over them — slide off to cancel. Volume and balance act
/// live while dragging. Seek previews while dragging and commits on release, so
/// scrubbing doesn't stutter the audio.
/// </para>
/// </summary>
public sealed class MainInput(MainViewState view)
{
    /// <summary>Balance snaps to centre within this many pixels of thumb travel.</summary>
    public const int BalanceSnapPixels = 1;

    private int _grabOffset;

    /// <summary>The widget holding the pointer between press and release.</summary>
    public MainWidget Captured { get; private set; }

    /// <summary>For a captured button: whether the pointer is still over it (drawn pressed).</summary>
    public bool PointerOverCaptured { get; private set; }

    /// <summary>For a captured slider: its value as dragged — 0..1, or -1..1 for balance.</summary>
    public double DragValue { get; private set; }

    public bool IsDragging(MainWidget slider) => Captured == slider;

    public bool IsPressed(MainWidget button) => Captured == button && PointerOverCaptured;

    public UiAction? PointerDown(int x, int y)
    {
        Reset();
        var widget = MainLayout.HitTest(x, y);

        switch (widget)
        {
            case MainWidget.None:
                return null;

            case MainWidget.TitleBar:
                return new UiAction(UiActionKind.DragWindow, widget);

            // Readouts respond on press, as the originals did.
            case MainWidget.TimeReadout:
            case MainWidget.Visualizer:
                return new UiAction(UiActionKind.Click, widget);

            case MainWidget.Volume:
                return BeginSlider(widget, MainLayout.VolumeSlider, view.Volume, x);

            case MainWidget.Balance:
                return BeginSlider(widget, MainLayout.BalanceSlider, (view.Balance + 1) / 2, x);

            case MainWidget.Seek:
                // Nothing loaded, nothing to seek: the thumb isn't even drawn.
                return view.CanSeek ? BeginSlider(widget, MainLayout.SeekSlider, view.SeekFraction, x) : null;

            default:
                Captured = widget;
                PointerOverCaptured = true;
                return null;
        }
    }

    public UiAction? PointerMove(int x, int y)
    {
        switch (Captured)
        {
            case MainWidget.None:
                return null;

            case MainWidget.Volume:
                return DragSlider(MainLayout.VolumeSlider, x);

            case MainWidget.Balance:
                return DragSlider(MainLayout.BalanceSlider, x);

            case MainWidget.Seek:
                DragSlider(MainLayout.SeekSlider, x);
                return null;

            default:
                PointerOverCaptured = MainLayout.RectOf(Captured).Contains(x, y);
                return null;
        }
    }

    public UiAction? PointerUp(int x, int y)
    {
        var captured = Captured;
        UiAction? result = null;

        switch (captured)
        {
            case MainWidget.None:
                break;

            case MainWidget.Seek:
                DragSlider(MainLayout.SeekSlider, x);
                result = new UiAction(UiActionKind.Seek, captured, DragValue);
                break;

            case MainWidget.Volume:
            case MainWidget.Balance:
                break;

            default:
                if (MainLayout.RectOf(captured).Contains(x, y))
                {
                    result = new UiAction(UiActionKind.Click, captured);
                }

                break;
        }

        Reset();
        return result;
    }

    /// <summary>Drops any capture without acting — e.g. after the OS took over for a window drag.</summary>
    public void Cancel() => Reset();

    private UiAction? BeginSlider(MainWidget widget, SliderGeometry slider, double fraction, int x)
    {
        Captured = widget;

        // Grabbing the thumb keeps the grab point under the pointer; clicking
        // the bare track jumps the thumb so its centre lands on the pointer.
        _grabOffset = slider.IsOnThumb(x, fraction) ? x - slider.ThumbX(fraction) : slider.ThumbWidth / 2;

        return DragSlider(slider, x);
    }

    private UiAction? DragSlider(SliderGeometry slider, int x)
    {
        var fraction = slider.FractionAt(x, _grabOffset);

        switch (Captured)
        {
            case MainWidget.Volume:
                DragValue = fraction;
                return new UiAction(UiActionKind.Volume, Captured, DragValue);

            case MainWidget.Balance:
                var centre = slider.Travel / 2.0;
                var thumb = fraction * slider.Travel;
                DragValue = Math.Abs(thumb - centre) <= BalanceSnapPixels ? 0 : (fraction * 2) - 1;
                return new UiAction(UiActionKind.Balance, Captured, DragValue);

            default:
                DragValue = fraction;
                return null;
        }
    }

    private void Reset()
    {
        Captured = MainWidget.None;
        PointerOverCaptured = false;
        DragValue = 0;
        _grabOffset = 0;
    }
}
