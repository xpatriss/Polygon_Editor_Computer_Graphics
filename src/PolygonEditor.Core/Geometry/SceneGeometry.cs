using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Geometry;

// Kopia samych pozycji pozwala sprawdzić zmianę i wycofać ją bez wymiany obiektów modelu.
public static class SceneGeometry
{
    public static double EdgeLength(Edge edge)
        => Math.Sqrt(HitTesting.DistanceSquared(edge.Start.Position, edge.End.Position));

    // Kopia obejmuje również uchwyty, żeby cofnięcie operacji odtwarzało całą geometrię.
    public static Dictionary<ScenePoint, Point2D> CapturePositions(Scene scene)
        => GetPoints(scene).ToDictionary(point => point, point => point.Position);

    public static IEnumerable<ScenePoint> GetPoints(Scene scene)
        => scene.Polygons.SelectMany(polygon => polygon.Vertices.Cast<ScenePoint>()
            .Concat(polygon.Edges.Where(edge => edge.Bezier is not null)
                .SelectMany(edge => new ScenePoint[] { edge.Bezier!.FirstControl, edge.Bezier.SecondControl }))
            .Concat(polygon.Edges.Where(edge => edge.Arc is not null).Select(edge => (ScenePoint)edge.Arc!.Center))).Distinct();

    // Wspólna polilinia dla rysowania, trafiania i sprawdzania wnętrza.
    public static IEnumerable<Point2D> SampleEdge(Edge edge)
    {
        if (edge.Arc is not null) return CircularArcGeometry.Sample(edge);
        if (edge.Bezier is not { } curve)
            return new[] { edge.Start.Position, edge.End.Position };
        var p0 = edge.Start.Position;
        var p1 = curve.FirstControl.Position;
        var p2 = curve.SecondControl.Position;
        var p3 = edge.End.Position;
        double length = Math.Sqrt(HitTesting.DistanceSquared(p0, p1))
            + Math.Sqrt(HitTesting.DistanceSquared(p1, p2)) + Math.Sqrt(HitTesting.DistanceSquared(p2, p3));
        int steps = (int)Math.Clamp(Math.Ceiling(length / 2), 16, 2048);
        return BezierCurve.Sample(p0, p1, p2, p3, steps);
    }

    internal static void RestorePositions(IReadOnlyDictionary<ScenePoint, Point2D> positions)
    {
        foreach (var (vertex, position) in positions)
            vertex.Position = position;
    }

    public static IEnumerable<(Point2D Start, Point2D End)> EdgeSegments(Edge edge)
    {
        using var points = SampleEdge(edge).GetEnumerator();
        if (!points.MoveNext())
            yield break;
        var previous = points.Current;
        while (points.MoveNext())
        {
            yield return (previous, points.Current);
            previous = points.Current;
        }
    }
}
