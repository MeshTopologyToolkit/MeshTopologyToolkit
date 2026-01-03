using Avalonia;
using Avalonia.Controls;

namespace MeshTopologyToolkit.Viewer;

// 2. Standard Avalonia App Entry Point
class App : Application
{
    private FileContainer _content;

    public App(FileContainer content)
    {
        this._content = content;
    }

    public override void Initialize() => Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {

            desktop.MainWindow = new SceneViewerWindow(_content)
            {
                Title = "Mesh Topology Toolkit Viewer",
                Width = 800,
                Height = 600,
                WindowState = WindowState.Maximized
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
