using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace PolygonEditor.App.Dialogs;

// Proste okno edycji jednej liczby. Wynik null oznacza anulowanie.
public sealed class LengthDialog : Window
{
    public LengthDialog(double currentLength)
    {
        Title = "Długość krawędzi";
        Width = 340;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var input = new TextBox { Text = currentLength.ToString("G17", CultureInfo.CurrentCulture) };
        var error = new TextBlock { Foreground = Brushes.Crimson, TextWrapping = TextWrapping.Wrap };
        var accept = new Button { Content = "Zapisz" };
        var cancel = new Button { Content = "Anuluj" };
        Content = new StackPanel
        {
            Margin = new Thickness(16), Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Wymagana długość (jednostki sceny):" }, input, error,
                new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
                    Children = { accept, cancel } }
            }
        };

        // Akceptujemy separator bieżącej kultury oraz kropkę, bez separatorów tysięcy.
        void Accept()
        {
            bool parsed = double.TryParse(input.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
                || double.TryParse(input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            if (!parsed || !double.IsFinite(value) || value <= 0)
            {
                error.Text = "Wpisz dodatnią, skończoną liczbę.";
                return;
            }
            Close((double?)value);
        }
        accept.Click += (_, _) => Accept();
        cancel.Click += (_, _) => Close((double?)null);
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                Accept();
            else if (e.Key == Key.Escape)
                Close((double?)null);
            else
                return;
            e.Handled = true;
        };
        Opened += (_, _) => { input.Focus(); input.SelectAll(); };
    }
}
