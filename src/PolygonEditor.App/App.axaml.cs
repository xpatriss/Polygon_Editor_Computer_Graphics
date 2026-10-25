using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PolygonEditor.App.Views;

namespace PolygonEditor.App;

public partial class App : Application
{
    public override void Initialize()
    {
        // wczytujemy ustawienia aplikacji i motyw z pliku App.axaml
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // główne okno tworzymy dopiero po zainicjalizowaniu frameworka
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
