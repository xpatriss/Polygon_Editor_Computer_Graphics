using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Editing;
using GeometryEditor = PolygonEditor.Core.Editing.PolygonEditor;

namespace PolygonEditor.App.Interaction;

// Stan zaznaczenia i gestu jest poza modelem geometrii. Klasa nie zależy od zdarzeń Avalonii.
public sealed class EditorInteraction
{
    // Aktualne zaznaczenie; null oznacza, że niczego nie wybrano.
    public HitResult? Selection { get; private set; }
    public Edge? SecondaryEdge { get; private set; }
    // Gest trwa od naciśnięcia do puszczenia; próg ruchu sprawdzamy osobno.
    public bool IsDragging { get; private set; }
    public string Message { get; private set; } = "Kliknij wierzchołek, krawędź lub wnętrze wielokąta.";
    private Point2D dragStart;
    private const double DragThreshold = 3;
    private bool dragThresholdReached;
    private Dictionary<Vertex, Point2D> initialPositions = new();
    private Scene? scene;

    // Podłącza model i usuwa stan zaznaczenia z poprzedniej sceny.
    public void SetScene(Scene scene)
    {
        Clear();
        this.scene = scene;
    }

    // Wybiera element pod kursorem i zapamiętuje pozycje z początku gestu.
    // Samo naciśnięcie nie przesuwa elementów.
    public void BeginDrag(Scene scene, Point2D point)
    {
        EndDrag();
        this.scene = scene;
        SecondaryEdge = null;
        Selection = HitTesting.Pick(scene, point);
        if (Selection is null)
        {
            Message = "Brak zaznaczenia.";
            return;
        }
        dragStart = point;
        // Kopia całej sceny zapobiega dryfowi także w wielokątach połączonych relacjami.
        initialPositions = SceneGeometry.CapturePositions(scene);
        IsDragging = true;
        Message = Selection.Vertex is not null ? "Wybrano wierzchołek. Delete — usuń."
            : Selection.Edge is not null ? "Wybrano krawędź. Insert — dodaj wierzchołek w środku."
            : "Wybrano wielokąt. Przeciągnij, aby go przesunąć.";
    }

    // Ctrl + klik wybiera drugą krawędź bez zmiany pierwszej i bez rozpoczynania ruchu.
    public void SelectSecondEdge(Scene scene, Point2D point)
    {
        EndDrag();
        this.scene = scene;
        if (Selection?.Edge is not { } first || scene.FindEdge(first.Id) != first)
        {
            Message = "Najpierw zwykłym kliknięciem wybierz pierwszą krawędź.";
            return;
        }
        var second = HitTesting.Pick(scene, point)?.Edge;
        if (second is null || second == first)
        {
            Message = "Ctrl + klik wybiera drugą, inną krawędź; kliknij z dala od wierzchołków.";
            return;
        }
        SecondaryEdge = second;
        Message = "Wybrano dwie krawędzie. Możesz dodać równoległość.";
    }

    // Po przekroczeniu progu przesuwa wybrany wierzchołek albo cały wielokąt.
    // Krawędź pozostaje zaznaczona, ale przeciągnięcie jej przesuwa cały wielokąt.
    public void Move(Point2D point)
    {
        if (!IsDragging || Selection is null || scene is null)
            return;
        // Kliknięcie (także z drobnym drżeniem myszy) ma wyłącznie zaznaczać.
        // Po rozpoczęciu przeciągania aktywny wierzchołek podąża dokładnie za kursorem.
        if (!dragThresholdReached)
        {
            if (HitTesting.DistanceSquared(point, dragStart) < DragThreshold * DragThreshold)
                return;
            dragThresholdReached = true;
        }
        var requested = new Dictionary<Vertex, Point2D>();
        if (Selection.Vertex is not null)
        {
            // Aktywny wierzchołek znajduje się dokładnie pod kursorem.
            requested[Selection.Vertex] = point;
        }
        else
        {
            // Przypinamy cały przesuwany wielokąt; solver może dostosować pozostałe wielokąty.
            double dx = point.X - dragStart.X;
            double dy = point.Y - dragStart.Y;
            foreach (var vertex in Selection.Polygon.Vertices)
            {
                var original = initialPositions[vertex];
                requested[vertex] = new Point2D(original.X + dx, original.Y + dy);
            }
        }
        if (!SceneEditor.TryMove(scene, initialPositions, requested))
            Message = "Nie udało się zachować relacji dla tej pozycji. Pozostawiono ostatnią poprawną scenę.";
        else if (scene.Constraints.Count > 0)
            Message = "Przesuwanie z zachowaniem relacji.";
    }

    // Dzieli zaznaczoną krawędź i zaznacza nowy wierzchołek.
    // Zwraca: true po dodaniu, false gdy nie zaznaczono krawędzi.
    public bool InsertVertex()
    {
        EndDrag();
        if (Selection?.Edge is not { } edge || scene is null)
        {
            Message = "Najpierw zaznacz krawędź.";
            return false;
        }
        var vertex = GeometryEditor.InsertMidpoint(scene, Selection.Polygon, edge);
        SecondaryEdge = null;
        Selection = new HitResult(Selection.Polygon, Vertex: vertex);
        Message = "Dodano wierzchołek w środku krawędzi.";
        return true;
    }

    // Usuwa zaznaczony wierzchołek; po usunięciu zaznaczony pozostaje wielokąt.
    // Zwraca: false przy braku wierzchołka lub próbie zmniejszenia trójkąta.
    public bool DeleteVertex()
    {
        EndDrag();
        if (Selection?.Vertex is not { } vertex || scene is null)
        {
            Message = "Najpierw zaznacz wierzchołek.";
            return false;
        }
        if (!GeometryEditor.RemoveVertex(scene, Selection.Polygon, vertex))
        {
            Message = "Wielokąt musi mieć co najmniej trzy wierzchołki.";
            return false;
        }
        Selection = new HitResult(Selection.Polygon);
        SecondaryEdge = null;
        Message = "Usunięto wierzchołek i połączono sąsiednie krawędzie.";
        return true;
    }

    // Przekazuje żądaną długość do Core i zachowuje komunikat z wynikiem próby.
    public bool SetLength(double length)
    {
        EndDrag();
        if (scene is null || Selection?.Edge is not { } edge)
        {
            Message = "Najpierw zaznacz krawędź.";
            return false;
        }
        bool result = ConstraintEditor.TrySetLength(scene, edge, length, out var message);
        Message = message;
        return result;
    }

    // Zleca połączenie dwóch zaznaczonych krawędzi relacją równoległości.
    public bool AddParallel()
    {
        EndDrag();
        if (scene is null || Selection?.Edge is not { } first || SecondaryEdge is not { } second)
        {
            Message = "Wybierz pierwszą krawędź, a następnie drugą przez Ctrl + klik.";
            return false;
        }
        bool result = ConstraintEditor.TryAddParallel(scene, first, second, out var message);
        Message = message;
        return result;
    }

    // Usuwa jedną relację wskazaną przez panel.
    public void RemoveConstraint(GeometricConstraint constraint)
    {
        EndDrag();
        Message = scene is not null && ConstraintEditor.Remove(scene, constraint)
            ? "Usunięto relację. Geometria pozostaje w aktualnym położeniu." : "Ta relacja nie należy do sceny.";
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
        SecondaryEdge = null;
        Message = "Brak zaznaczenia.";
    }
}
