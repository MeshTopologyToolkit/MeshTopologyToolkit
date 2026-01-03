using Avalonia.Controls;
using Avalonia.Input;

namespace MeshTopologyToolkit.Viewer;

public class SceneViewerWindow: Window
{
    SceneViewerControl _control;
    public SceneViewerWindow(FileContainer content)
    {
        _control = new SceneViewerControl(content)
        {
            Focusable = true,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        Content = _control;
        _control.Focus();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        _control.HandlePointerMoved(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _control.HandlePointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _control.HandlePointerReleased(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _control.HandleKeyDown(e);
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _control.HandleKeyUp(e);
        base.OnKeyUp(e);
    }

}
