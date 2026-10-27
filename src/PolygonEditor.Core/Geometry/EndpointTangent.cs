using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Geometry;

// Styczna jako przeskalowana różnica punktów, opcjonalnie obrócona o 90 stopni.
// Taki zapis pozwala uwzględnić także wpływ środka łuku w solverze.
public readonly record struct EndpointTangent(ScenePoint From, ScenePoint To, double Scale, bool Rotated = false)
{
    public Point2D Vector
    {
        get
        {
            double x = Scale * (To.Position.X - From.Position.X);
            double y = Scale * (To.Position.Y - From.Position.Y);
            return Rotated ? new Point2D(-y, x) : new Point2D(x, y);
        }
    }

    public static EndpointTangent For(Edge edge, bool atStart)
    {
        if (edge.Arc is { } arc)
            return new EndpointTangent(arc.Center, atStart ? edge.Start : edge.End, arc.Direction, true);
        if (edge.Bezier is { } curve)
            return atStart ? new EndpointTangent(edge.Start, curve.FirstControl, 3)
                : new EndpointTangent(curve.SecondControl, edge.End, 3);
        return new EndpointTangent(edge.Start, edge.End, 1);
    }
}
