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
    private Dictionary<ScenePoint, Point2D> initialPositions = new();
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
        Message = Selection.ControlPoint is not null ? "Wybrano uchwyt Béziera. Przeciągnij, aby zmienić krzywą."
            : Selection.Vertex is not null ? "Wybrano wierzchołek. Delete — usuń."
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
        if (first.Kind != EdgeKind.Line || second.Kind != EdgeKind.Line)
        {
            SecondaryEdge = null;
            Message = "Do równoległości wybierz dwa odcinki; krzywe łączymy warunkami ciągłości.";
            return;
        }
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
        var requested = new Dictionary<ScenePoint, Point2D>();
        if (Selection.ControlPoint is not null)
        {
            requested[Selection.ControlPoint] = point;
        }
        else if (Selection.Vertex is not null)
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
            // Przy przesuwaniu całego wielokąta jego uchwyty przesuwają się razem z obwodem.
            foreach (var curve in Selection.Polygon.Edges.Where(edge => edge.Bezier is not null).Select(edge => edge.Bezier!))
                foreach (var control in new[] { curve.FirstControl, curve.SecondControl })
                {
                    var original = initialPositions[control];
                    requested[control] = new Point2D(original.X + dx, original.Y + dy);
                }
            foreach (var edge in Selection.Polygon.Edges.Where(edge => edge.Arc is not null))
            {
                var center = edge.Arc!.Center;
                var original = initialPositions[center];
                requested[center] = new Point2D(original.X + dx, original.Y + dy);
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
        Vertex vertex;
        try
        {
            vertex = GeometryEditor.InsertMidpoint(scene, Selection.Polygon, edge);
        }
        catch (InvalidOperationException error)
        {
            Message = error.Message;
            return false;
        }
        SecondaryEdge = null;
        Selection = new HitResult(Selection.Polygon, Vertex: vertex);
        Message = "Dodano wierzchołek w środku odcinka lub w t=0.5 krzywej.";
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
            Message = Selection.Polygon.Vertices.Count == 3 ? "Wielokąt musi mieć co najmniej trzy wierzchołki."
                : "Nie udało się zachować ciągłości po usunięciu. Zmianę cofnięto.";
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

    public bool SetEdgeKind(EdgeKind kind)
    {
        EndDrag();
        if (scene is null || Selection?.Edge is not { } edge)
        {
            Message = "Najpierw wybierz krawędź.";
            return false;
        }
        bool result = CurveEditor.TrySetEdgeKind(scene, edge, kind, out var message);
        Message = message;
        // Zmiana typu może usunąć uchwyt; po niej zaznaczenie wskazuje samą krawędź.
        if (result)
        {
            Selection = new HitResult(Selection.Polygon, Edge: edge);
            SecondaryEdge = null;
        }
        return result;
    }

    public bool SetContinuity(ContinuityClass continuity)
    {
        EndDrag();
        if (scene is null || Selection?.Vertex is not { } vertex)
        {
            Message = "Najpierw wybierz wierzchołek.";
            return false;
        }
        bool result = CurveEditor.TrySetContinuity(scene, vertex, continuity, out var message);
        Message = message;
        return result;
    }

    public bool FlipArc()
    {
        EndDrag();
        if (scene is null || Selection?.Edge is not { } edge)
        {
            Message = "Najpierw wybierz łuk.";
            return false;
        }
        bool result = CurveEditor.TryFlipArc(scene, edge, out var message);
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
