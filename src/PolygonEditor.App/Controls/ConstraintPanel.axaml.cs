using Avalonia.Controls;
using PolygonEditor.App.Dialogs;
using PolygonEditor.App.Rendering;
using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.App.Controls;

// Łączy panel relacji z płótnem. Solver i zmiany modelu pozostają w Core.
public partial class ConstraintPanel : UserControl
{
    public ConstraintPanel() => InitializeComponent();

    // Podłączamy raz przy tworzeniu okna; później reagujemy na zmianę stanu płótna.
    public void Connect(SceneCanvas canvas, Scene scene, Window owner)
    {
        var edgeInfo = RequireControl<TextBlock>("EdgeInfo");
        var lengthButton = RequireControl<Button>("LengthButton");
        var parallelButton = RequireControl<Button>("ParallelButton");
        var list = RequireControl<ListBox>("ConstraintsList");
        var removeButton = RequireControl<Button>("RemoveConstraintButton");

        void Update()
        {
            var selected = canvas.SelectedEdge;
            edgeInfo.Text = selected is null ? "Nie wybrano krawędzi."
                : selected.Kind != EdgeKind.Line ? "Wybrano krzywą. Długość i równoległość dotyczą odcinków."
                : $"Długość wybranej: {SceneGeometry.EdgeLength(selected):0.##}. "
                    + (canvas.SecondSelectedEdge is null ? "Brak drugiej krawędzi." : "Wybrano też drugą krawędź.");
            lengthButton.IsEnabled = selected?.Kind == EdgeKind.Line;
            parallelButton.IsEnabled = selected?.Kind == EdgeKind.Line && canvas.SecondSelectedEdge?.Kind == EdgeKind.Line;
            var selectedId = (list.SelectedItem as ConstraintListItem)?.Constraint.Id;
            var items = scene.Constraints.Select((constraint, index) =>
                new ConstraintListItem(constraint, ConstraintLabels.Description(scene, constraint, index))).ToArray();
            list.ItemsSource = items;
            list.SelectedItem = items.FirstOrDefault(item => item.Constraint.Id == selectedId);
            removeButton.IsEnabled = list.SelectedItem is not null;
        }

        canvas.SelectionChanged += (_, _) => Update();
        list.SelectionChanged += (_, _) => removeButton.IsEnabled = list.SelectedItem is not null;
        lengthButton.Click += async (_, _) =>
        {
            if (canvas.SelectedEdge is not { } edge)
                return;
            var dialog = new LengthDialog(SceneGeometry.EdgeLength(edge));
            double? length = await dialog.ShowDialog<double?>(owner);
            if (length.HasValue)
                canvas.SetLength(length.Value);
            else
                canvas.Focus();
        };
        parallelButton.Click += (_, _) => canvas.AddParallel();
        removeButton.Click += (_, _) =>
        {
            if (list.SelectedItem is ConstraintListItem item)
                canvas.RemoveConstraint(item.Constraint);
        };
        Update();
    }

    private T RequireControl<T>(string name) where T : Control
        => this.FindControl<T>(name) ?? throw new InvalidOperationException($"{name} was not found in ConstraintPanel.");

}

public sealed record ConstraintListItem(GeometricConstraint Constraint, string Text);
