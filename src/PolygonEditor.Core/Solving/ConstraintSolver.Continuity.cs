using PolygonEditor.Core.Model;
using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Solving;

// Druga część tej samej klasy: styczne i warunki G1/C1, bez mieszania ich z obsługą myszy.
public static partial class ConstraintSolver
{
    private static IEnumerable<(Vertex Vertex, Edge Incoming, Edge Outgoing)> ContinuityJoints(Scene scene)
    {
        foreach (var polygon in scene.Polygons)
        {
            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                var vertex = polygon.Vertices[i];
                var incoming = polygon.Edges[(i + polygon.Edges.Count - 1) % polygon.Edges.Count];
                var outgoing = polygon.Edges[i];
                // G0 zapewnia wspólny obiekt Vertex. Wyższe klasy stosujemy przy krzywej.
                if (vertex.Continuity != ContinuityClass.G0 && (incoming.Kind != EdgeKind.Line || outgoing.Kind != EdgeKind.Line))
                    yield return (vertex, incoming, outgoing);
            }
        }
    }

    private static bool ContinuitiesSatisfied(Scene scene)
        => ContinuityJoints(scene).All(joint => EvaluateContinuity(joint.Vertex, joint.Incoming, joint.Outgoing).All(IsSatisfied));

    // Na końcach Béziera pochodne to 3*(P1-P0) i 3*(P3-P2); dla odcinka koniec-początek.
    private static IEnumerable<Equation?> EvaluateContinuity(Vertex vertex, Edge incoming, Edge outgoing)
    {
        var first = EndpointTangent.For(incoming, false);
        var second = EndpointTangent.For(outgoing, true);
        var u = first.Vector;
        var v = second.Vector;
        double ux = u.X, uy = u.Y, vx = v.X, vy = v.Y;

        if (vertex.Continuity == ContinuityClass.C1)
        {
            if (incoming.Arc is not null || outgoing.Arc is not null)
            {
                yield return null;
                yield break;
            }
            // C1: obie składowe wektorów stycznych mają być równe, nie tylko ich kierunek.
            var xGradients = new Dictionary<ScenePoint, (double X, double Y)>();
            AddTangentGradient(xGradients, first, 1, 0);
            AddTangentGradient(xGradients, second, -1, 0);
            yield return new Equation(ux - vx, LengthTolerance, xGradients);

            // Liczymy ponownie Y po ewentualnej korekcie X; współrzędne są tu niezależne.
            var yGradients = new Dictionary<ScenePoint, (double X, double Y)>();
            AddTangentGradient(yGradients, first, 0, 1);
            AddTangentGradient(yGradients, second, 0, -1);
            yield return new Equation(first.Vector.Y - second.Vector.Y, LengthTolerance, yGradients);
            yield break;
        }

        double firstSquared = ux * ux + uy * uy;
        double secondSquared = vx * vx + vy * vy;
        if (firstSquared < MinimumLength * MinimumLength || secondSquared < MinimumLength * MinimumLength)
        {
            // G1 wymaga niezerowych wektorów stycznych.
            yield return null;
            yield break;
        }
        double angle = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
        var gradients = new Dictionary<ScenePoint, (double X, double Y)>();
        // W G1 styczne muszą mieć ten sam zwrot: nie sprowadzamy kąta modulo pi jak dla równoległości.
        double gux = uy / firstSquared, guy = -ux / firstSquared;
        double gvx = -vy / secondSquared, gvy = vx / secondSquared;
        AddTangentGradient(gradients, first, gux, guy);
        AddTangentGradient(gradients, second, gvx, gvy);
        yield return new Equation(angle, AngleTolerance, gradients);
    }

    private static void AddTangentGradient(Dictionary<ScenePoint, (double X, double Y)> gradients,
        EndpointTangent tangent, double x, double y)
    {
        // Przy stycznej łuku cofamy obrót o 90 stopni w pochodnej (reguła łańcuchowa).
        if (tangent.Rotated) (x, y) = (y, -x);
        AddGradient(gradients, tangent.From, -tangent.Scale * x, -tangent.Scale * y);
        AddGradient(gradients, tangent.To, tangent.Scale * x, tangent.Scale * y);
    }
}
