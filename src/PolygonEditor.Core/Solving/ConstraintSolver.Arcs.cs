using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Solving;

public static partial class ConstraintSolver
{
    internal static bool ValidateArcRules(Scene scene, out string message)
    {
        // To reguły zadania, a nie błędy do zmniejszania iteracyjnie.
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges).Where(edge => edge.Arc is not null))
        {
            if (edge.Start.Continuity == ContinuityClass.C1 || edge.End.Continuity == ContinuityClass.C1)
            {
                message = "Przy łuku dostępne są tylko G0 i G1. Zmień C1 przed włączeniem łuku.";
                return false;
            }
            if (edge.Start.Continuity == ContinuityClass.G1 && edge.End.Continuity == ContinuityClass.G1)
            {
                message = "Tylko jeden koniec łuku może mieć G1. Na drugim ustaw G0.";
                return false;
            }
            if (SceneGeometry.EdgeLength(edge) < MinimumLength)
            {
                message = "Łuk wymaga dwóch różnych położeń końców.";
                return false;
            }
        }
        message = "";
        return true;
    }

    private static bool UpdateSemicircleCenters(Scene scene, IReadOnlySet<ScenePoint>? pinned)
    {
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges))
        {
            if (edge.Arc is not { } arc || edge.Start.Continuity == ContinuityClass.G1 || edge.End.Continuity == ContinuityClass.G1)
                continue;
            var middle = BezierCurve.Lerp(edge.Start.Position, edge.End.Position, 0.5);
            if (pinned?.Contains(arc.Center) == true && HitTesting.DistanceSquared(middle, arc.Center.Position) > LengthTolerance * LengthTolerance)
                return false;
            // Przy G0/G0 środek jest wielkością zależną: aktualizujemy tylko jego, bez ruchu końców.
            arc.Center.Position = middle;
        }
        return true;
    }

    private static IEnumerable<Equation?> ArcEquations(Scene scene)
    {
        foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges))
        {
            if (edge.Arc is not { } arc || (edge.Start.Continuity != ContinuityClass.G1 && edge.End.Continuity != ContinuityClass.G1))
                continue;
            double ax = edge.Start.Position.X - arc.Center.Position.X;
            double ay = edge.Start.Position.Y - arc.Center.Position.Y;
            double bx = edge.End.Position.X - arc.Center.Position.X;
            double by = edge.End.Position.Y - arc.Center.Position.Y;
            // (ax, ay) i (bx, by) to promienie od środka do obu końców.
            // first i second są ich długościami.
            double first = Math.Sqrt(ax * ax + ay * ay), second = Math.Sqrt(bx * bx + by * by);
            if (first < MinimumLength || second < MinimumLength
                || Math.Max(first, second) > CircularArcGeometry.MaximumRadiusRatio * SceneGeometry.EdgeLength(edge))
            {
                yield return null;
                continue;
            }
            var gradients = new Dictionary<ScenePoint, (double X, double Y)>();
            // Oba końce muszą leżeć na tym samym okręgu: różnica promieni ma być zerowa.
            AddGradient(gradients, edge.Start, ax / first, ay / first);
            AddGradient(gradients, edge.End, -bx / second, -by / second);
            // Ruch środka zmienia obie odległości, więc sumujemy oba wpływy.
            AddGradient(gradients, arc.Center, -ax / first + bx / second, -ay / first + by / second);
            yield return new Equation(first - second, LengthTolerance, gradients);
        }
    }

    private static bool ArcShapesSatisfied(Scene scene)
        => ArcEquations(scene).All(IsSatisfied) && scene.Polygons.SelectMany(polygon => polygon.Edges)
            .Where(edge => edge.Arc is not null).All(edge => CircularArcGeometry.TryDescribe(edge, out _));
}
