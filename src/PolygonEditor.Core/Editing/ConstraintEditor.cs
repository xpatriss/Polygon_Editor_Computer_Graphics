using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Solving;

namespace PolygonEditor.Core.Editing;

// Najpierw sprawdza geometrię z próbną relacją, dopiero potem zapisuje ją w scenie.
public static class ConstraintEditor
{
    // Ustawienie długości zastępuje poprzednią długość tej samej krawędzi.
    public static bool TrySetLength(Scene scene, Edge edge, double length, out string message)
    {
        if (scene.FindEdge(edge.Id) != edge || !double.IsFinite(length) || length <= 0)
        {
            message = "Długość musi być dodatnią, skończoną liczbą, a krawędź należeć do sceny.";
            return false;
        }
        // szuykamy poprzedniego ograniczenia długości, jeśli nie było to null
        var previous = scene.Constraints.OfType<LengthConstraint>().FirstOrDefault(item => item.EdgeId == edge.Id);
        // tworzymy nowe ograniczenie
        var constraint = new LengthConstraint(edge.Id, length);
        // tworzymy próbny zestaw nowych ograniczeń sceny
        var candidate = scene.Constraints.Where(item => item != previous).Append(constraint).ToArray();
        // solver próbuje dostosować scenę do nowego zestawu ograniczeń
        // przy nadawaniu długości zachowujemy początek wybranej krawędzi
        if (!ConstraintSolver.TrySolve(scene, new HashSet<Vertex> { edge.Start }, candidate))
        {
            message = "Nie udało się uzgodnić długości z pozostałymi relacjami. Zmianę cofnięto.";
            return false;
        }
        // nadpisujemy stare ograniczenia nowymi
        if (previous is not null)
            scene.RemoveConstraint(previous);
        scene.AddConstraint(constraint);
        message = "Zapisano ograniczenie długości.";
        return true;
    }

    // Próbuje dodać równoległość, zachowując podczas tej operacji końce pierwszej krawędzi.
    public static bool TryAddParallel(Scene scene, Edge first, Edge second, out string message)
    {
        if (first == second || scene.FindEdge(first.Id) != first || scene.FindEdge(second.Id) != second
            || SceneGeometry.EdgeLength(first) < 1e-8 || SceneGeometry.EdgeLength(second) < 1e-8)
        {
            message = "Wybierz dwie różne krawędzie o niezerowej długości.";
            return false;
        }
        if (scene.Constraints.OfType<ParallelConstraint>().Any(item => item.EdgeIds.Contains(first.Id) && item.EdgeIds.Contains(second.Id)))
        {
            message = "Te krawędzie mają już relację równoległości.";
            return false;
        }
        var constraint = new ParallelConstraint(first.Id, second.Id);
        var candidate = scene.Constraints.Append(constraint).ToArray();
        // Pierwsza krawędź jest odniesieniem podczas dodawania, ale nie jest trwale przypięta.
        if (!ConstraintSolver.TrySolve(scene, new HashSet<Vertex> { first.Start, first.End }, candidate))
        {
            message = "Nie udało się uzgodnić równoległości z pozostałymi relacjami. Zmianę cofnięto.";
            return false;
        }
        scene.AddConstraint(constraint);
        message = "Dodano równoległość krawędzi.";
        return true;
    }

    // Usuwa wskazaną relację bez cofania dotychczasowych zmian geometrii.
    public static bool Remove(Scene scene, GeometricConstraint constraint)
        => scene.RemoveConstraint(constraint);
}
