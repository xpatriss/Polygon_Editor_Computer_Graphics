using Avalonia.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using PolygonEditor.App.Interaction;
using PolygonEditor.Core.Geometry;
using PolygonEditor.App.Rendering;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Constraints;

namespace PolygonEditor.App.Controls;

// Łączy model sceny z odświeżaniem kontrolki; rysowanie wykonuje SceneRenderer.
public sealed class SceneCanvas : Control
{
    private Scene? scene;
    private readonly SceneRenderer renderer = new SceneRenderer();
    private readonly EditorInteraction interaction = new();
    private IPointer? capturedPointer;
    private LineDrawingAlgorithm lineAlgorithm = LineDrawingAlgorithm.Library;
    public event EventHandler? SelectionChanged;
    public string SelectionDescription => interaction.Message;
    public Edge? SelectedEdge => interaction.Selection?.Edge;
    public Edge? SecondSelectedEdge => interaction.SecondaryEdge;
    public Vertex? SelectedVertex => interaction.Selection?.Vertex;
    public bool SelectedVertexHasArc => SelectedVertex is { } vertex && scene is not null
        && scene.Polygons.SelectMany(polygon => polygon.Edges)
            .Any(edge => edge.Arc is not null && (edge.Start == vertex || edge.End == vertex));

    public SceneCanvas()
    {
        Focusable = true;
    }

    // Zmienia tylko sposób rysowania i przywraca obsługę skrótów klawiatury na płótnie.
    public void SetLineAlgorithm(LineDrawingAlgorithm algorithm)
    {
        lineAlgorithm = algorithm;
        Focus();
        InvalidateVisual();
    }

    // Podłącza nową scenę, czyści zaznaczenie i zgłasza odświeżenie.
    public void SetScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        StopDragging();
        interaction.SetScene(scene);
        this.scene = scene;
        Refresh();
    }

    // Avalonia wywołuje tę metodę, aby narysować scenę i bieżące zaznaczenie.
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // Przezroczyste tło zapewnia odbieranie kliknięć również pomiędzy odcinkami.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));
        if (scene == null)
            return;
        renderer.Draw(context, scene, interaction.Selection, lineAlgorithm, interaction.SecondaryEdge);
    }

    // Lewy przycisk wybiera element. Przechwycenie myszy pozwala przeciągać poza płótnem.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (scene is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        Focus();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            interaction.SelectSecondEdge(scene, ToPoint(e.GetPosition(this)));
            Refresh();
            e.Handled = true;
            return;
        }
        interaction.BeginDrag(scene, ToPoint(e.GetPosition(this)));
        if (interaction.IsDragging)
        {
            capturedPointer = e.Pointer;
            e.Pointer.Capture(this);
        }
        Refresh();
        e.Handled = true;
    }

    // Przekazuje nową pozycję kursora do obsługi gestu i zgłasza ponowne rysowanie.
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!interaction.IsDragging || capturedPointer != e.Pointer)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            StopDragging();
        else
            interaction.Move(ToPoint(e.GetPosition(this)));
        Refresh();
    }

    // Uwzględnia ostatnią pozycję kursora, kończy gest i zwalnia przechwycenie myszy.
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left || capturedPointer != e.Pointer)
            return;
        interaction.Move(ToPoint(e.GetPosition(this)));
        StopDragging();
        Refresh();
        e.Handled = true;
    }

    // Kończy gest także wtedy, gdy przechwycenie myszy zostanie utracone poza zwykłym puszczeniem.
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        capturedPointer = null;
        interaction.EndDrag();
    }

    // Obsługuje Insert, Delete i Escape, gdy płótno ma fokus klawiatury.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Insert)
            InsertVertex();
        else if (e.Key == Key.Delete)
            DeleteVertex();
        else if (e.Key == Key.Escape)
        {
            StopDragging();
            interaction.Clear();
            Refresh();
        }
        else
            return;
        e.Handled = true;
    }

    // Wspólna obsługa klawisza Insert i przycisku dodawania wierzchołka.
    public void InsertVertex()
    {
        StopDragging();
        interaction.InsertVertex();
        Focus();
        Refresh();
    }

    // Wspólna obsługa klawisza Delete i przycisku usuwania wierzchołka.
    public void DeleteVertex()
    {
        StopDragging();
        interaction.DeleteVertex();
        Focus();
        Refresh();
    }

    // Kończy gest, zleca zmianę długości i odświeża scenę oraz panel.
    public void SetLength(double length)
    {
        StopDragging();
        interaction.SetLength(length);
        Focus();
        Refresh();
    }

    // Dodaje relację dla wybranej pary krawędzi i pokazuje wynik próby.
    public void AddParallel()
    {
        StopDragging();
        interaction.AddParallel();
        Focus();
        Refresh();
    }

    public void SetEdgeKind(EdgeKind kind)
    {
        StopDragging();
        interaction.SetEdgeKind(kind);
        Focus();
        Refresh();
    }

    public void SetContinuity(ContinuityClass continuity)
    {
        StopDragging();
        interaction.SetContinuity(continuity);
        Focus();
        Refresh();
    }

    public void FlipArc()
    {
        StopDragging();
        interaction.FlipArc();
        Focus();
        Refresh();
    }

    // Wspólna obsługa usuwania relacji z panelu i odświeżenia jej oznaczeń.
    public void RemoveConstraint(GeometricConstraint constraint)
    {
        StopDragging();
        interaction.RemoveConstraint(constraint);
        Focus();
        Refresh();
    }

    // Zatrzymuje gest i oddaje przechwyconą mysz, zachowując zaznaczenie.
    private void StopDragging()
    {
        interaction.EndDrag();
        var pointer = capturedPointer;
        capturedPointer = null;
        pointer?.Capture(null);
    }

    // Zgłasza ponowne rysowanie i powiadamia okno o zmianie opisu zaznaczenia.
    private void Refresh()
    {
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static Point2D ToPoint(Point point) => new(point.X, point.Y);
}
