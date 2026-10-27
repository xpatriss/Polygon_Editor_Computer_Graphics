using Avalonia.Controls;
using PolygonEditor.Core.Model;

namespace PolygonEditor.App.Controls;

// Panel przekazuje polecenia do płótna; nie oblicza krzywych ani stycznych.
public partial class CurvePanel : UserControl
{
    public CurvePanel() => InitializeComponent();

    public void Connect(SceneCanvas canvas)
    {
        var info = RequireControl<TextBlock>("CurveInfo");
        var toggle = RequireControl<Button>("ToggleBezierButton");
        var arcToggle = RequireControl<Button>("ToggleArcButton");
        var flip = RequireControl<Button>("FlipArcButton");
        var choice = RequireControl<ComboBox>("ContinuityChoice");
        var apply = RequireControl<Button>("SetContinuityButton");
        choice.ItemsSource = Enum.GetValues<ContinuityClass>();

        void Update()
        {
            var edge = canvas.SelectedEdge;
            var vertex = canvas.SelectedVertex;
            toggle.IsEnabled = edge is not null;
            toggle.Content = edge?.Kind == EdgeKind.CubicBezier ? "Zamień na odcinek" : "Zamień na Béziera";
            arcToggle.IsEnabled = edge is not null;
            arcToggle.Content = edge?.Kind == EdgeKind.CircularArc ? "Łuk → odcinek" : "Zamień na łuk";
            flip.IsEnabled = edge?.Arc is not null && edge.Start.Continuity == ContinuityClass.G0
                && edge.End.Continuity == ContinuityClass.G0;
            choice.ItemsSource = canvas.SelectedVertexHasArc
                ? new[] { ContinuityClass.G0, ContinuityClass.G1 } : Enum.GetValues<ContinuityClass>();
            choice.IsEnabled = vertex is not null;
            apply.IsEnabled = vertex is not null;
            choice.SelectedItem = vertex?.Continuity;
            info.Text = vertex is not null
                ? $"Ciągłość: {vertex.Continuity} ({(vertex.IsContinuityExplicit ? "jawny wybór" : "domyślna")})."
                : edge is not null ? $"Wybrana krawędź: {edge.Kind switch { EdgeKind.CubicBezier => "Bézier trzeciego stopnia", EdgeKind.CircularArc => "łuk okręgu", _ => "odcinek" }}."
                : "Wybierz krawędź lub wierzchołek.";
        }

        canvas.SelectionChanged += (_, _) => Update();
        toggle.Click += (_, _) =>
        {
            if (canvas.SelectedEdge is { } edge)
                canvas.SetEdgeKind(edge.Kind == EdgeKind.CubicBezier ? EdgeKind.Line : EdgeKind.CubicBezier);
        };
        arcToggle.Click += (_, _) =>
        {
            if (canvas.SelectedEdge is { } edge)
                canvas.SetEdgeKind(edge.Kind == EdgeKind.CircularArc ? EdgeKind.Line : EdgeKind.CircularArc);
        };
        flip.Click += (_, _) => canvas.FlipArc();
        apply.Click += (_, _) =>
        {
            if (choice.SelectedItem is ContinuityClass continuity)
                canvas.SetContinuity(continuity);
        };
        Update();
    }

    private T RequireControl<T>(string name) where T : Control
        => this.FindControl<T>(name) ?? throw new InvalidOperationException($"{name} was not found in CurvePanel.");
}
