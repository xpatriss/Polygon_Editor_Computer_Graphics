using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Geometry;

// Kopia samych pozycji pozwala sprawdzić zmianę i wycofać ją bez wymiany obiektów modelu.
public static class SceneGeometry
{
    public static double EdgeLength(Edge edge)
        => Math.Sqrt(HitTesting.DistanceSquared(edge.Start.Position, edge.End.Position));

    public static Dictionary<Vertex, Point2D> CapturePositions(Scene scene)
        => scene.Polygons.SelectMany(polygon => polygon.Vertices).Distinct()
            .ToDictionary(vertex => vertex, vertex => vertex.Position);

    internal static void RestorePositions(IReadOnlyDictionary<Vertex, Point2D> positions)
    {
        foreach (var (vertex, position) in positions)
            vertex.Position = position;
    }
}
