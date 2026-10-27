using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model;

// Wspólna pozycja i tożsamość punktów przesuwanych przez edytor i solver.
public abstract class ScenePoint
{
    public Guid Id { get; } = Guid.NewGuid();
    public Point2D Position { get; internal set; }

    protected ScenePoint(Point2D position) => Position = position;
}
