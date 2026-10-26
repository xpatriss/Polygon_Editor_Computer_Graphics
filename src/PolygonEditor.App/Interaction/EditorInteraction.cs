using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using GeometryEditor = PolygonEditor.Core.Editing.PolygonEditor;

namespace PolygonEditor.App.Interaction;

// Stan zaznaczenia i gestu jest poza modelem geometrii. Klasa nie zależy od zdarzeń Avalonii.
public sealed class EditorInteraction
{
    // Aktualne zaznaczenie; null oznacza, że niczego nie wybrano.
    public HitResult? Selection { get; private set; }
    // Gest trwa od naciśnięcia do puszczenia; próg ruchu sprawdzamy osobno.
    public bool IsDragging { get; private set; }
    public string Message { get; private set; } = "Kliknij wierzchołek, krawędź lub wnętrze wielokąta.";
    private Point2D dragStart;
    private const double DragThreshold = 3;
    private bool dragThresholdReached;
    private readonly Dictionary<Vertex, Point2D> initialPositions = new();

    // Wybiera element pod kursorem i zapamiętuje pozycje z początku gestu.
    // Samo naciśnięcie nie przesuwa elementów.
    public void BeginDrag(Scene scene, Point2D point)
    {
        EndDrag();
        Selection = HitTesting.Pick(scene, point);
        if (Selection is null)
        {
            Message = "Brak zaznaczenia.";
            return;
        }
        dragStart = point;
        foreach (var vertex in Selection.Polygon.Vertices)
            initialPositions.Add(vertex, vertex.Position);
        IsDragging = true;
        Message = Selection.Vertex is not null ? "Wybrano wierzchołek. Delete — usuń."
            : Selection.Edge is not null ? "Wybrano krawędź. Insert — dodaj wierzchołek w środku."
            : "Wybrano wielokąt. Przeciągnij, aby go przesunąć.";
    }

    // Po przekroczeniu progu przesuwa wybrany wierzchołek albo cały wielokąt.
    // Krawędź pozostaje zaznaczona, ale przeciągnięcie jej przesuwa cały wielokąt.
    public void Move(Point2D point)
    {
        if (!IsDragging || Selection is null)
            return;
        // Kliknięcie (także z drobnym drżeniem myszy) ma wyłącznie zaznaczać.
        // Po rozpoczęciu przeciągania aktywny wierzchołek podąża dokładnie za kursorem.
        if (!dragThresholdReached)
        {
            if (HitTesting.DistanceSquared(point, dragStart) < DragThreshold * DragThreshold)
                return;
            dragThresholdReached = true;
        }
        if (Selection.Vertex is not null)
        {
            // Aktywny wierzchołek znajduje się dokładnie pod kursorem.
            GeometryEditor.MoveVertex(Selection.Vertex, point);
            return;
        }
        // Każdy ruch liczymy od początku gestu, co zapobiega kumulowaniu przesunięć.
        double dx = point.X - dragStart.X;
        double dy = point.Y - dragStart.Y;
        foreach (var (vertex, original) in initialPositions)
            GeometryEditor.MoveVertex(vertex, new Point2D(original.X + dx, original.Y + dy));
    }

    // Dzieli zaznaczoną krawędź i zaznacza nowy wierzchołek.
    // Zwraca: true po dodaniu, false gdy nie zaznaczono krawędzi.
    public bool InsertVertex()
    {
        EndDrag();
        if (Selection?.Edge is not { } edge)
        {
            Message = "Najpierw zaznacz krawędź.";
            return false;
        }
        var vertex = GeometryEditor.InsertMidpoint(Selection.Polygon, edge);
        Selection = new HitResult(Selection.Polygon, Vertex: vertex);
        Message = "Dodano wierzchołek w środku krawędzi.";
        return true;
    }

    // Usuwa zaznaczony wierzchołek; po usunięciu zaznaczony pozostaje wielokąt.
    // Zwraca: false przy braku wierzchołka lub próbie zmniejszenia trójkąta.
    public bool DeleteVertex()
    {
        EndDrag();
        if (Selection?.Vertex is not { } vertex)
        {
            Message = "Najpierw zaznacz wierzchołek.";
            return false;
        }
        if (!GeometryEditor.RemoveVertex(Selection.Polygon, vertex))
        {
            Message = "Wielokąt musi mieć co najmniej trzy wierzchołki.";
            return false;
        }
        Selection = new HitResult(Selection.Polygon);
        Message = "Usunięto wierzchołek i połączono sąsiednie krawędzie.";
        return true;
    }

    // Kończy gest i usuwa zapamiętane pozycje; zachowuje zaznaczenie.
    public void EndDrag()
    {
        IsDragging = false;
        dragThresholdReached = false;
        initialPositions.Clear();
    }

    // Kończy gest i usuwa zaznaczenie, bez cofania zmian geometrii.
    public void Clear()
    {
        EndDrag();
        Selection = null;
        Message = "Brak zaznaczenia.";
    }
}
