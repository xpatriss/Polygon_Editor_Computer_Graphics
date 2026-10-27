using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Editing;

// Zapis do cofnięcia zmiany typu krawędzi, ciągłości lub obwodu, bez wymiany istniejących obiektów.
internal sealed class SceneEditSnapshot
{
    private readonly Scene scene;
    private readonly Dictionary<ScenePoint, Point2D> positions;
    private readonly Dictionary<Polygon, (Vertex[] Vertices, Edge[] Edges)> topology;
    private readonly Dictionary<Edge, BezierSegment?> curves;
    private readonly Dictionary<Edge, (ArcSegment? Arc, int Direction)> arcs;
    private readonly Dictionary<Vertex, (ContinuityClass Class, bool Explicit)> continuity;
    private readonly GeometricConstraint[] constraints;

    public SceneEditSnapshot(Scene scene)
    {
        this.scene = scene;
        positions = SceneGeometry.CapturePositions(scene);
        topology = scene.Polygons.Distinct().ToDictionary(polygon => polygon, polygon => polygon.CaptureTopology());
        curves = scene.Polygons.SelectMany(polygon => polygon.Edges).Distinct().ToDictionary(edge => edge, edge => edge.Bezier);
        arcs = scene.Polygons.SelectMany(polygon => polygon.Edges).Distinct()
            .ToDictionary(edge => edge, edge => (edge.Arc, edge.Arc?.Direction ?? 1));
        continuity = scene.Polygons.SelectMany(polygon => polygon.Vertices).Distinct()
            .ToDictionary(vertex => vertex, vertex => (vertex.Continuity, vertex.IsContinuityExplicit));
        constraints = scene.Constraints.ToArray();
    }

    public void Restore()
    {
        foreach (var (polygon, saved) in topology)
            polygon.RestoreTopology(saved.Vertices, saved.Edges);
        foreach (var (edge, curve) in curves)
            edge.Bezier = curve;
        foreach (var (edge, saved) in arcs)
        {
            edge.Arc = saved.Arc;
            if (edge.Arc is not null) edge.Arc.Direction = saved.Direction;
        }
        foreach (var (vertex, saved) in continuity)
        {
            vertex.Continuity = saved.Class;
            vertex.IsContinuityExplicit = saved.Explicit;
        }
        scene.RestoreConstraints(constraints);
        SceneGeometry.RestorePositions(positions);
    }
}
