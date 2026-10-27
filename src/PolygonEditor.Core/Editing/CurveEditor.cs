using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Solving;

namespace PolygonEditor.Core.Editing;

public static class CurveEditor
{
    public static bool TrySetEdgeKind(Scene scene, Edge edge, EdgeKind kind, out string message)
    {
        if (scene.FindEdge(edge.Id) != edge || !Enum.IsDefined(kind))
        {
            message = "Wybierz krawędź należącą do sceny.";
            return false;
        }
        if (edge.Kind == kind)
        {
            message = "Krawędź ma już wybrany typ.";
            return true;
        }
        if (kind != EdgeKind.Line && SceneGeometry.EdgeLength(edge) < 1e-8)
        {
            message = "Najpierw rozsuń końce krawędzi zerowej długości.";
            return false;
        }
        var before = new SceneEditSnapshot(scene);
        edge.Bezier = kind == EdgeKind.CubicBezier ? new BezierSegment(
            BezierCurve.Lerp(edge.Start.Position, edge.End.Position, 1.0 / 3),
            BezierCurve.Lerp(edge.Start.Position, edge.End.Position, 2.0 / 3)) : null;
        edge.Arc = kind == EdgeKind.CircularArc ? new ArcSegment(BezierCurve.Lerp(edge.Start.Position, edge.End.Position, 0.5)) : null;
        if (edge.Arc is not null)
        {
            // Łuk zaczyna od G0/G0, o ile użytkownik nie zapisał jawnie innego wyboru.
            if (!edge.Start.IsContinuityExplicit) edge.Start.Continuity = ContinuityClass.G0;
            if (!edge.End.IsContinuityExplicit) edge.End.Continuity = ContinuityClass.G0;
        }
        // Długość i równoległość mają znaczenie dla odcinków.
        scene.RemoveConstraintsForEdges(new[] { edge.Id });
        ApplyDefaults(scene);
        if (!ConstraintSolver.ValidateArcRules(scene, out message))
        {
            before.Restore();
            return false;
        }
        SeedArcs(scene);
        // Zmiana typu zachowuje wierzchołki. Ciągłość dopasowuje wolne uchwyty.
        if (!ConstraintSolver.TrySolve(scene, VertexPins(scene)))
        {
            before.Restore();
            message = "Nie udało się uzgodnić ciągłości po zmianie typu. Przywrócono poprzednią scenę.";
            return false;
        }
        message = kind switch
        {
            EdgeKind.Line => "Zamieniono krzywą na odcinek.",
            EdgeKind.CircularArc => "Włączono łuk. G0/G0 daje półokrąg; G1 można ustawić na jednym końcu.",
            _ => "Włączono Béziera; domyślna ciągłość to G1."
        };
        return true;
    }

    public static bool TrySetContinuity(Scene scene, Vertex vertex, ContinuityClass continuity, out string message)
    {
        if (!scene.Polygons.Any(polygon => polygon.Vertices.Contains(vertex)) || !Enum.IsDefined(continuity))
        {
            message = "Wybierz wierzchołek sceny i poprawną klasę ciągłości.";
            return false;
        }
        var before = new SceneEditSnapshot(scene);
        vertex.Continuity = continuity;
        vertex.IsContinuityExplicit = true;
        if (!ConstraintSolver.ValidateArcRules(scene, out message))
        {
            before.Restore();
            return false;
        }
        SeedArcs(scene);
        if (!ConstraintSolver.TrySolve(scene, VertexPins(scene)))
        {
            before.Restore();
            message = "Nie udało się ustawić tej ciągłości. Przywrócono poprzednią geometrię.";
            return false;
        }
        message = $"Zapisano jawny wybór {continuity}. Jest stosowany, gdy wierzchołek sąsiaduje z krzywą.";
        return true;
    }

    public static bool TryFlipArc(Scene scene, Edge edge, out string message)
    {
        if (scene.FindEdge(edge.Id) != edge || edge.Arc is null
            || edge.Start.Continuity != ContinuityClass.G0 || edge.End.Continuity != ContinuityClass.G0)
        {
            message = "Stronę półokręgu zmieniamy przy G0 na obu końcach. Przy G1 wynika ona ze stycznej.";
            return false;
        }
        var before = new SceneEditSnapshot(scene);
        edge.Arc.Direction *= -1;
        if (!ConstraintSolver.TrySolve(scene, VertexPins(scene)))
        {
            before.Restore();
            message = "Nie udało się zmienić strony łuku. Zmianę cofnięto.";
            return false;
        }
        message = "Zmieniono stronę półokręgu.";
        return true;
    }

    private static void SeedArcs(Scene scene)
    {
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges).Where(edge => edge.Arc is not null))
            CircularArcGeometry.SeedFromNeighbor(scene, edge);
    }

    // domyślne G1, bez nadpisywania jawnego wyboru użytkownika
    internal static void ApplyDefaults(Scene scene)
    {
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges).Where(edge => edge.Bezier is not null))
        {
            if (!edge.Start.IsContinuityExplicit)
                edge.Start.Continuity = ContinuityClass.G1;
            if (!edge.End.IsContinuityExplicit)
                edge.End.Continuity = ContinuityClass.G1;
        }
    }

    internal static HashSet<ScenePoint> VertexPins(Scene scene)
        => scene.Polygons.SelectMany(polygon => polygon.Vertices).Cast<ScenePoint>().ToHashSet();
}
