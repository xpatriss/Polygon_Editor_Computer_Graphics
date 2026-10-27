using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Solving;

namespace PolygonEditor.Core.Editing;

// Jakikolwiek ruch jest próbą zmiany całej sceny, bo relacje mogą łączyć różne wielokąty.
public static class SceneEditor
{
    // Liczy ruch od początku gestu. Przy braku rozwiązania zachowuje ostatnią poprawną scenę.
    public static bool TryMove(Scene scene, IReadOnlyDictionary<ScenePoint, Point2D> initialPositions,
        IReadOnlyDictionary<ScenePoint, Point2D> requestedPositions)
    {
        // Zapisujemy ostatnią poprawną geometrię, aby móc wycofać tę próbę ruchu
        // - robimy kopię aktualnych współrzędnych wszystkich wierzchołków i uchwytów
        var previous = SceneGeometry.CapturePositions(scene);
        // sprawdzamy poprawność requestedPositions
        if (requestedPositions.Any(item => !previous.ContainsKey(item.Key)
            || !double.IsFinite(item.Value.X) || !double.IsFinite(item.Value.Y)))
            throw new ArgumentException("Requested vertices must belong to the scene and have finite positions.", nameof(requestedPositions));
        // sprawdzamy zapis kopii początkowej pozycji
        if (initialPositions.Count != previous.Count || initialPositions.Keys.Any(vertex => !previous.ContainsKey(vertex)))
            throw new ArgumentException("Initial positions must cover the current scene.", nameof(initialPositions));
        // przywracamy początkowe pozycje, a następnie nakładamy żądane pozycje dla wybranych punktów
        SceneGeometry.RestorePositions(initialPositions);
        // Uchwyt przy przesuwanym wierzchołku najpierw przesuwamy o ten sam wektor.
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges))
        {
            if (edge.Arc is { } arc)
            {
                // Wstępny środek przesuwa się o średnią przesunięć końców.
                // Dla G0 solver ustawi dokładny środek cięciwy; dla G1 dopasuje okrąg.
                var original = initialPositions[arc.Center];
                double dx = 0, dy = 0;
                foreach (var endpoint in new[] { edge.Start, edge.End })
                    if (requestedPositions.TryGetValue(endpoint, out var target))
                    {
                        dx += (target.X - initialPositions[endpoint].X) / 2;
                        dy += (target.Y - initialPositions[endpoint].Y) / 2;
                    }
                arc.Center.Position = new Point2D(original.X + dx, original.Y + dy);
            }
            if (edge.Bezier is not { } curve)
                continue;
            if (requestedPositions.TryGetValue(edge.Start, out var start))
                TranslateControl(curve.FirstControl, edge.Start, start, initialPositions);
            if (requestedPositions.TryGetValue(edge.End, out var end))
                TranslateControl(curve.SecondControl, edge.End, end, initialPositions);
        }
        foreach (var (vertex, position) in requestedPositions)
            vertex.Position = position;
        // solver próbuje ustawić nowe pozycje
        var pinned = requestedPositions.Keys.ToHashSet();
        // Przy ruchu uchwytu najpierw chronimy oba końce jego segmentu.
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges))
        {
            if (edge.Bezier is { } curve && (requestedPositions.ContainsKey(curve.FirstControl)
                || requestedPositions.ContainsKey(curve.SecondControl)))
            {
                pinned.Add(edge.Start);
                pinned.Add(edge.End);
            }
        }
        if (ConstraintSolver.TrySolve(scene, pinned))
            return true;
        // Wyjątek: gdy dodatkowe przypięcia uniemożliwiają uzgodnienie relacji, pozwalamy ruszyć końce.
        // Sam przeciągany punkt nadal pozostaje dokładnie w żądanej pozycji.
        if (pinned.Count > requestedPositions.Count && ConstraintSolver.TrySolve(scene, requestedPositions.Keys.ToHashSet()))
            return true;
        // wracamy do stanu sprzed tej próby
        SceneGeometry.RestorePositions(previous);
        return false;
    }

    private static void TranslateControl(ControlPoint control, Vertex vertex, Point2D target,
        IReadOnlyDictionary<ScenePoint, Point2D> initial)
    {
        var source = initial[vertex];
        var original = initial[control];
        control.Position = new Point2D(original.X + target.X - source.X, original.Y + target.Y - source.Y);
    }
}
