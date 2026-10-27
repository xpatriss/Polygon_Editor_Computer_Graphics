using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Solving;

namespace PolygonEditor.Core.Editing;

// Jakikolwiek ruch jest próbą zmiany całej sceny, bo relacje mogą łączyć różne wielokąty.
public static class SceneEditor
{
    // Liczy ruch od początku gestu. Przy braku rozwiązania zachowuje ostatnią poprawną scenę.
    public static bool TryMove(Scene scene, IReadOnlyDictionary<Vertex, Point2D> initialPositions,
        IReadOnlyDictionary<Vertex, Point2D> requestedPositions)
    {
        // Zapisujemy ostatnią poprawną geometrię, aby móc wycofać tę próbę ruchu
        // - robimy kopię aktualnych współrzędnych wszystkich wierzchołków
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
        foreach (var (vertex, position) in requestedPositions)
            vertex.Position = position;
        // solver próbuje ustawić nowe pozycje
        if (ConstraintSolver.TrySolve(scene, requestedPositions.Keys.ToHashSet()))
            return true;
        // wracamy do stanu sprzed tej próby
        SceneGeometry.RestorePositions(previous);
        return false;
    }
}
