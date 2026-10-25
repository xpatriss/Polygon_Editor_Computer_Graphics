using Avalonia.Controls;
using PolygonEditor.App.Controls;
using PolygonEditor.Core;

namespace PolygonEditor.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // układ okna z MainWindow.axaml, logika geometrii w Core
        InitializeComponent();

        // Kontrolka istnieje po wczytaniu XAML; odnajdujemy ją po x:Name.
        var canvas = this.FindControl<SceneCanvas>("EditorCanvas");
        if (canvas is null)
            throw new InvalidOperationException("EditorCanvas control was not found in MainWindow.");

        // Scenę tworzymy raz, aby odświeżanie zachowywało tożsamość jej elementów.
        var scene = DemoSceneFactory.Create();
        canvas.SetScene(scene);
    }
}
