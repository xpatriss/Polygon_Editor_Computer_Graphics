using System.Globalization;
using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Model;

namespace PolygonEditor.App.Rendering;

// Wspólne oznaczenia na scenie i w panelu; oba końce relacji równoległości mają ten sam numer.
public static class ConstraintLabels
{
    public static string Marker(GeometricConstraint constraint, int index) => constraint switch
    {
        LengthConstraint length => $"L{index + 1}={length.TargetLength.ToString("0.##", CultureInfo.CurrentCulture)}",
        ParallelConstraint => $"∥{index + 1}",
        _ => "?"
    };

    public static string Description(Scene scene, GeometricConstraint constraint, int index)
    {
        var edges = constraint.EdgeIds.Select(id => DescribeEdge(scene, id));
        string kind = constraint is LengthConstraint ? "Długość" : "Równoległość";
        return $"{Marker(constraint, index)} · {kind}: {string.Join("; ", edges)}";
    }

    private static string DescribeEdge(Scene scene, Guid id)
    {
        for (int p = 0; p < scene.Polygons.Count; p++)
            for (int e = 0; e < scene.Polygons[p].Edges.Count; e++)
                if (scene.Polygons[p].Edges[e].Id == id)
                    return $"W{p + 1}/K{e + 1}";
        return "usunięta krawędź";
    }
}
