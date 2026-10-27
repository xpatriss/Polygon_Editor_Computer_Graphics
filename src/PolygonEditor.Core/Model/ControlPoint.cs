using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model;

// Uchwyt krzywej jest punktem sceny, ale nie jest wierzchołkiem obwodu wielokąta.
public sealed class ControlPoint : ScenePoint
{
    public ControlPoint(Point2D position) : base(position) { }
}
