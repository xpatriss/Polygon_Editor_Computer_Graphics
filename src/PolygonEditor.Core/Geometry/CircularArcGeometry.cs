using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Geometry;

public static class CircularArcGeometry
{
    public readonly record struct Description(Point2D Center, double Radius, double StartAngle, double Sweep);
    public const double MaximumRadiusRatio = 100000;

    public static bool TryDescribe(Edge edge, out Description description)
    {
        description = default;
        if (edge.Arc is not { } arc || (arc.Direction != 1 && arc.Direction != -1)) return false;
        var a = edge.Start.Position;
        var b = edge.End.Position;
        var center = arc.Center.Position;
        if (edge.Start.Continuity == ContinuityClass.G0 && edge.End.Continuity == ContinuityClass.G0
            && HitTesting.DistanceSquared(center, BezierCurve.Lerp(a, b, 0.5)) > 1e-8) return false;
        double chord = SceneGeometry.EdgeLength(edge);
        double radius = Math.Sqrt(HitTesting.DistanceSquared(a, center));
        double endRadius = Math.Sqrt(HitTesting.DistanceSquared(b, center));
        // Ograniczamy prawie prosty łuk o olbrzymim promieniu, żeby obliczenia pozostały stabilne.
        if (!double.IsFinite(radius) || !double.IsFinite(endRadius) || chord < 1e-8 || radius < 1e-8
            || radius > MaximumRadiusRatio * chord || Math.Abs(radius - endRadius) > 0.0002) return false;
        // atan2 podaje kierunek promienia. Sweep to zakres obiegu od A do B ze znakiem.
        double startAngle = Math.Atan2(a.Y - center.Y, a.X - center.X);
        double endAngle = Math.Atan2(b.Y - center.Y, b.X - center.X);
        double sweep = arc.Direction > 0 ? PositiveAngle(endAngle - startAngle) : -PositiveAngle(startAngle - endAngle);
        if (Math.Abs(sweep) < 1e-12) return false;
        description = new Description(center, radius, startAngle, sweep);
        return true;
    }

    private static double PositiveAngle(double angle)
    {
        angle %= 2 * Math.PI;
        return angle < 0 ? angle + 2 * Math.PI : angle;
    }

    public static Point2D PointAt(Edge edge, double t)
    {
        if (!double.IsFinite(t) || t < 0 || t > 1) throw new ArgumentOutOfRangeException(nameof(t));
        if (!TryDescribe(edge, out var arc)) throw new InvalidOperationException("Niepoprawna geometria łuku.");
        if (t == 0) return edge.Start.Position;
        if (t == 1) return edge.End.Position;
        // Zapis względem początku ogranicza odejmowanie dużych, prawie równych współrzędnych.
        double angle = arc.Sweep * t;
        double x = edge.Start.Position.X - arc.Center.X;
        double y = edge.Start.Position.Y - arc.Center.Y;
        double cosineMinusOne = -2 * Math.Pow(Math.Sin(angle / 2), 2);
        double sine = Math.Sin(angle);
        return new Point2D(edge.Start.Position.X + cosineMinusOne * x - sine * y,
            edge.Start.Position.Y + sine * x + cosineMinusOne * y);
    }

    public static IEnumerable<Point2D> Sample(Edge edge)
    {
        if (!TryDescribe(edge, out var arc)) throw new InvalidOperationException("Niepoprawna geometria łuku.");
        // Kroki ograniczają przybliżoną długość fragmentu i zmianę kąta.
        int steps = (int)Math.Clamp(Math.Ceiling(Math.Max(arc.Radius * Math.Abs(arc.Sweep) / 2,
            Math.Abs(arc.Sweep) / 0.05)), 8, 4096);
        double angle = arc.Sweep / steps;
        double cosine = Math.Cos(angle), sine = Math.Sin(angle);
        double x = edge.Start.Position.X - arc.Center.X;
        double y = edge.Start.Position.Y - arc.Center.Y;
        yield return edge.Start.Position;
        for (int step = 1; step < steps; step++)
        {
            // Obrót wektora promienia o stały kąt, bez ponownego liczenia sin/cos w pętli.
            double nextX = cosine * x - sine * y;
            y = sine * x + cosine * y;
            x = nextX;
            yield return new Point2D(arc.Center.X + x, arc.Center.Y + y);
        }
        yield return edge.End.Position;
    }

    // Przy G1 wstępnie wyznaczamy środek z cięciwy i kierunku sąsiedniej stycznej.
    // Właściwą zgodność wszystkich relacji potwierdza później solver.
    public static void SeedFromNeighbor(Scene scene, Edge edge)
    {
        if (edge.Arc is not { } arc) return;
        bool atStart = edge.Start.Continuity == ContinuityClass.G1;
        if (!atStart && edge.End.Continuity != ContinuityClass.G1) return;
        var polygon = scene.Polygons.First(polygon => polygon.Edges.Contains(edge));
        int index = polygon.Edges.ToList().IndexOf(edge);
        var neighbor = polygon.Edges[(index + (atStart ? polygon.Edges.Count - 1 : 1)) % polygon.Edges.Count];
        var tangent = EndpointTangent.For(neighbor, !atStart).Vector;
        double length = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (length < 1e-8) return;
        var anchor = atStart ? edge.Start.Position : edge.End.Position;
        var other = atStart ? edge.End.Position : edge.Start.Position;
        double dx = other.X - anchor.X, dy = other.Y - anchor.Y;
        // (nx, ny) jest jednostkową normalną do wymaganej stycznej.
        double nx = -tangent.Y / length, ny = tangent.X / length;
        // Środek leży na normalnej. Z równości promieni wynika podana odległość offset.
        double denominator = 2 * (dx * nx + dy * ny);
        if (Math.Abs(denominator) > 1e-8 * Math.Sqrt(dx * dx + dy * dy))
        {
            double offset = (dx * dx + dy * dy) / denominator;
            if (Math.Abs(offset) <= MaximumRadiusRatio * Math.Sqrt(dx * dx + dy * dy))
            {
                arc.Center.Position = new Point2D(anchor.X + offset * nx, anchor.Y + offset * ny);
                arc.Direction = Math.Sign(offset);
                return;
            }
        }
        // Gdy sąsiad też jest krzywą, solver może dopasować jego swobodną styczną.
        var current = EndpointTangent.For(edge, atStart).Vector;
        if (current.X * tangent.X + current.Y * tangent.Y < 0) arc.Direction *= -1;
    }
}
