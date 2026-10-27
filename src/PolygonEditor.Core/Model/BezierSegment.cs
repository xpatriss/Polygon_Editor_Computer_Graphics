using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model;

// Końce krzywej należą do Edge; tutaj przechowujemy dwa dodatkowe uchwyty.
public sealed class BezierSegment
{
    public ControlPoint FirstControl { get; }
    public ControlPoint SecondControl { get; }

    public BezierSegment(Point2D first, Point2D second)
    {
        FirstControl = new ControlPoint(first);
        SecondControl = new ControlPoint(second);
    }
}
