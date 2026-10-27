using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace PolygonEditor.App.Views;

// Pomoc korzysta z plików osadzonych w aplikacji. Nie wymaga przeglądarki ani biblioteki Markdown.
public sealed class HelpWindow : Window
{
    public HelpWindow()
    {
        Title = "Dokumentacja edytora wielokątów";
        Width = 820;
        Height = 650;
        MinWidth = 500;
        MinHeight = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new TabControl
        {
            Margin = new Thickness(12),
            ItemsSource = new[]
            {
                CreateTab("Obsługa", "Obsluga.md"),
                CreateTab("Założenia i model", "Model.md"),
                CreateTab("Algorytmy i źródła", "Algorytmy.md")
            }
        };
        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) { Close(); args.Handled = true; }
        };
    }

    private static TabItem CreateTab(string title, string fileName)
    {
        string resourceName = $"PolygonEditor.App.Help.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded help: {resourceName}");
        using var reader = new StreamReader(stream);
        return new TabItem
        {
            Header = title,
            Content = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = new SelectableTextBlock
                {
                    Text = reader.ReadToEnd(),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 15,
                    Margin = new Thickness(12)
                }
            }
        };
    }
}
