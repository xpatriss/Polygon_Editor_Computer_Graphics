using PolygonEditor.App.Interaction;
using PolygonEditor.Core;
using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Editing;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Solving;
using GeometryEditor = PolygonEditor.Core.Editing.PolygonEditor;

internal static class ConstraintChecks
{
    public static void Run(Action<string, Action> check)
    {
        check("Length can be set, replaced and combined with parallelism", () =>
        {
            var (scene, first, second) = Pair();
            Require(ConstraintEditor.TrySetLength(scene, first, 80, out var message), message);
            Near(SceneGeometry.EdgeLength(first), 80);
            Require(ConstraintEditor.TrySetLength(scene, first, 120, out message), message);
            Require(scene.Constraints.Count == 1, "Replacing length left two length constraints.");
            Near(SceneGeometry.EdgeLength(first), 120);
            Require(ConstraintEditor.TryAddParallel(scene, first, second, out message), message);
            Require(scene.Constraints.Count == 2 && ConstraintSolver.AreSatisfied(scene), "Combined constraints failed.");
            Require(((ICollection<GeometricConstraint>)scene.Constraints).IsReadOnly, "Constraint view is writable.");
        });

        check("Invalid lengths and foreign edges leave the scene untouched", () =>
        {
            var (scene, first, _) = Pair();
            var before = SceneGeometry.CapturePositions(scene);
            foreach (double length in new[] { 0, -1, double.NaN, double.PositiveInfinity })
                Require(!ConstraintEditor.TrySetLength(scene, first, length, out _), "Invalid length was accepted.");
            Require(!ConstraintEditor.TrySetLength(scene, Square(500, 0).Edges[0], 30, out _), "Foreign edge accepted.");
            SamePositions(before);
            Require(scene.Constraints.Count == 0, "Invalid relation was stored.");
        });

        check("A collapsed edge can receive a positive length", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(0, 0), V(30, 30) });
            var scene = new Scene(new[] { polygon });
            Require(ConstraintEditor.TrySetLength(scene, polygon.Edges[0], 20, out var message), message);
            Near(SceneGeometry.EdgeLength(polygon.Edges[0]), 20);
            Near(polygon.Vertices[0].Position.X, 0);
        });

        check("Parallelism rotates a perpendicular edge from another polygon", () =>
        {
            var first = Square(0, 0);
            var second = Square(200, 50);
            var scene = new Scene(new[] { first, second });
            var beforeA = first.Edges[0].Start.Position;
            var beforeB = first.Edges[0].End.Position;
            Require(ConstraintEditor.TryAddParallel(scene, first.Edges[0], second.Edges[1], out var message), message);
            Require(ConstraintSolver.AreSatisfied(scene), "Perpendicular initial directions did not converge.");
            Near(first.Edges[0].Start.Position.X, beforeA.X);
            Near(first.Edges[0].Start.Position.Y, beforeA.Y);
            Near(first.Edges[0].End.Position.X, beforeB.X);
            Near(first.Edges[0].End.Position.Y, beforeB.Y);
        });

        check("Opposite directions are parallel; duplicate and self relations are rejected", () =>
        {
            var polygon = Square(0, 0);
            var scene = new Scene(new[] { polygon });
            Require(ConstraintEditor.TryAddParallel(scene, polygon.Edges[0], polygon.Edges[2], out var message), message);
            Require(!ConstraintEditor.TryAddParallel(scene, polygon.Edges[2], polygon.Edges[0], out _), "Duplicate pair accepted.");
            Require(!ConstraintEditor.TryAddParallel(scene, polygon.Edges[0], polygon.Edges[0], out _), "Self pair accepted.");
            Require(scene.Constraints.Count == 1 && ConstraintSolver.AreSatisfied(scene), "Opposite directions failed.");
        });

        check("Dragging pins the cursor point and preserves length plus cross-polygon parallelism", () =>
        {
            var (scene, first, second) = ConstrainedPair();
            var before = SceneGeometry.CapturePositions(scene);
            var target = new Point2D(15, 35);
            Require(SceneEditor.TryMove(scene, before, new Dictionary<Vertex, Point2D> { [first.Start] = target }), "Move failed.");
            Near(first.Start.Position.X, target.X);
            Near(first.Start.Position.Y, target.Y);
            Near(SceneGeometry.EdgeLength(first), 100);
            Near(SceneGeometry.EdgeLength(second), 100);
            Require(ConstraintSolver.AreSatisfied(scene), "Drag broke a constraint.");
            var after = SceneGeometry.CapturePositions(scene);
            Require(SceneEditor.TryMove(scene, before, new Dictionary<Vertex, Point2D> { [first.Start] = target }), "Repeated move failed.");
            SamePositions(after);
        });

        check("A cycle of three parallel edges with fixed lengths remains solvable", () =>
        {
            var polygons = new[] { Square(0, 0), Square(200, 50), Square(400, 100) };
            var scene = new Scene(polygons);
            foreach (var polygon in polygons)
                Require(ConstraintEditor.TrySetLength(scene, polygon.Edges[0], 100, out var message), message);
            for (int i = 0; i < 3; i++)
                Require(ConstraintEditor.TryAddParallel(scene, polygons[i].Edges[0], polygons[(i + 1) % 3].Edges[0], out var message), message);
            var before = SceneGeometry.CapturePositions(scene);
            Require(SceneEditor.TryMove(scene, before, new Dictionary<Vertex, Point2D> { [polygons[0].Vertices[0]] = new(10, 40) }), "Cyclic move failed.");
            Require(ConstraintSolver.AreSatisfied(scene), "A cycle lost a relation.");
        });

        check("Impossible pinned movement restores the last valid geometry", () =>
        {
            var (scene, first, _) = ConstrainedPair();
            var initial = SceneGeometry.CapturePositions(scene);
            Require(SceneEditor.TryMove(scene, initial, new Dictionary<Vertex, Point2D> { [first.Start] = new(10, 20) }), "Valid setup move failed.");
            var lastValid = SceneGeometry.CapturePositions(scene);
            var impossible = new Dictionary<Vertex, Point2D> { [first.Start] = new(0, 0), [first.End] = new(300, 0) };
            Require(!SceneEditor.TryMove(scene, initial, impossible), "Impossible fixed endpoints were accepted.");
            SamePositions(lastValid);
            Require(ConstraintSolver.AreSatisfied(scene), "Failed movement damaged constraints.");
        });

        check("An impossible triangle length replacement keeps old relation and positions", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(100, 0), V(200, 0) });
            var scene = new Scene(new[] { polygon });
            foreach (var edge in polygon.Edges)
                Require(ConstraintEditor.TrySetLength(scene, edge, SceneGeometry.EdgeLength(edge), out var message), message);
            var before = SceneGeometry.CapturePositions(scene);
            var ids = scene.Constraints.Select(item => item.Id).ToArray();
            Require(!ConstraintEditor.TrySetLength(scene, polygon.Edges[0], 400, out _), "Impossible triangle inequality accepted.");
            SamePositions(before);
            Require(ids.SequenceEqual(scene.Constraints.Select(item => item.Id)), "Failed replacement changed stored relations.");
        });

        check("Adjacent edges share gradient contributions correctly", () =>
        {
            var polygon = Square(0, 0);
            var scene = new Scene(new[] { polygon });
            Require(ConstraintEditor.TryAddParallel(scene, polygon.Edges[0], polygon.Edges[1], out var message), message);
            Require(ConstraintSolver.AreSatisfied(scene), "Shared endpoint relation failed.");
            var initial = SceneGeometry.CapturePositions(scene);
            Require(SceneEditor.TryMove(scene, initial, new Dictionary<Vertex, Point2D> { [polygon.Vertices[1]] = new(100, 20) }), "Shared-vertex movement failed.");
            Require(ConstraintSolver.AreSatisfied(scene), "Shared-vertex gradient lost parallelism.");
        });

        check("Splitting removes affected relations but keeps unrelated edge identities", () =>
        {
            var scene = DemoSceneFactory.Create();
            var polygon = scene.Polygons[0];
            var kept = polygon.Edges[2];
            Require(ConstraintEditor.TrySetLength(scene, kept, 180, out var message), message);
            GeometryEditor.InsertMidpoint(scene, polygon, polygon.Edges[0]);
            Require(scene.Constraints.Count == 1 && scene.Constraints[0].EdgeIds.Contains(kept.Id), "Wrong constraints survived split.");
            Require(scene.FindEdge(kept.Id) == kept && ConstraintSolver.AreSatisfied(scene), "Unaffected edge was lost.");
        });

        check("Removing each vertex cleans both adjacent edges and keeps surviving relations", () =>
        {
            for (int index = 0; index < 4; index++)
            {
                var polygon = Square(0, 0);
                var scene = new Scene(new[] { polygon });
                foreach (var edge in polygon.Edges)
                    Require(ConstraintEditor.TrySetLength(scene, edge, 100, out var message), message);
                var vertex = polygon.Vertices[index];
                var removed = polygon.Edges.Where(edge => edge.Start == vertex || edge.End == vertex).Select(edge => edge.Id).ToHashSet();
                Require(GeometryEditor.RemoveVertex(scene, polygon, vertex), "Removal failed.");
                Require(scene.Constraints.Count == 2 && scene.Constraints.All(item => item.EdgeIds.All(id => !removed.Contains(id) && scene.FindEdge(id) != null)),
                    "Dangling or incorrectly removed relation.");
                Require(ConstraintSolver.AreSatisfied(scene), "Surviving relation was broken.");
            }
        });

        check("Rejecting triangle vertex removal leaves its constraints intact", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(100, 0), V(50, 50) });
            var scene = new Scene(new[] { polygon });
            Require(ConstraintEditor.TrySetLength(scene, polygon.Edges[0], 100, out var message), message);
            Require(!GeometryEditor.RemoveVertex(scene, polygon, polygon.Vertices[0]), "Triangle shrank.");
            Require(scene.Constraints.Count == 1 && ConstraintSolver.AreSatisfied(scene), "Rejected deletion removed constraints.");
        });

        check("Ctrl selection, adding and removing relations work through interaction", () =>
        {
            var (scene, first, second) = Pair();
            var interaction = new EditorInteraction();
            interaction.SetScene(scene);
            interaction.BeginDrag(scene, new(50, 0));
            interaction.EndDrag();
            interaction.SelectSecondEdge(scene, new(250, 50));
            Require(interaction.Selection?.Edge == first && interaction.SecondaryEdge == second && !interaction.IsDragging, "Pair selection failed.");
            Require(interaction.SetLength(100) && interaction.AddParallel(), interaction.Message);
            var relation = scene.Constraints.OfType<ParallelConstraint>().Single();
            interaction.RemoveConstraint(relation);
            Require(scene.Constraints.Count == 1, "Removing chosen relation removed the wrong relation.");
            interaction.Clear();
            Require(interaction.SecondaryEdge is null && !interaction.AddParallel(), "Clear left a second edge selected.");
        });

        check("A rigid triangle follows 120 cursor targets while preserving all three sides", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(100, 0), V(100, 100) });
            var scene = new Scene(new[] { polygon });
            foreach (var edge in polygon.Edges)
                Require(ConstraintEditor.TrySetLength(scene, edge, SceneGeometry.EdgeLength(edge), out var message), message);
            var before = SceneGeometry.CapturePositions(scene);
            for (int i = 0; i < 120; i++)
            {
                double angle = i * Math.PI / 60;
                var target = new Point2D(25 * Math.Cos(angle), 25 * Math.Sin(angle));
                Require(SceneEditor.TryMove(scene, before, new Dictionary<Vertex, Point2D> { [polygon.Vertices[0]] = target }),
                    $"Rigid triangle failed at cursor target {i}.");
                Near(polygon.Vertices[0].Position.X, target.X, 1e-10);
                Near(polygon.Vertices[0].Position.Y, target.Y, 1e-10);
                Require(ConstraintSolver.AreSatisfied(scene), "Rigid triangle lost a length.");
            }
        });

        check("An impossible parallel relation on a rigid triangle is rolled back", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(100, 0), V(50, 80) });
            var scene = new Scene(new[] { polygon });
            foreach (var edge in polygon.Edges)
                Require(ConstraintEditor.TrySetLength(scene, edge, SceneGeometry.EdgeLength(edge), out var message), message);
            var before = SceneGeometry.CapturePositions(scene);
            Require(!ConstraintEditor.TryAddParallel(scene, polygon.Edges[0], polygon.Edges[1], out _), "Impossible parallelism was accepted.");
            SamePositions(before);
            Require(scene.Constraints.Count == 3 && ConstraintSolver.AreSatisfied(scene), "Failed parallelism damaged the triangle.");
        });

        check("A zero-length edge cannot receive parallelism", () =>
        {
            var polygon = new Polygon(new[] { V(0, 0), V(0, 0), V(30, 30) });
            var scene = new Scene(new[] { polygon });
            var before = SceneGeometry.CapturePositions(scene);
            Require(!ConstraintEditor.TryAddParallel(scene, polygon.Edges[0], polygon.Edges[1], out _), "Undefined direction was accepted.");
            SamePositions(before);
            Require(scene.Constraints.Count == 0, "Invalid parallelism was stored.");
        });

        check("A whole constrained polygon translates without deformation", () =>
        {
            var (scene, _, _) = ConstrainedPair();
            var interaction = new EditorInteraction();
            var before = SceneGeometry.CapturePositions(scene);
            interaction.BeginDrag(scene, new(50, 50));
            interaction.Move(new(80, 70));
            foreach (var vertex in scene.Polygons[0].Vertices)
            {
                Near(vertex.Position.X, before[vertex].X + 30);
                Near(vertex.Position.Y, before[vertex].Y + 20);
            }
            Require(ConstraintSolver.AreSatisfied(scene), "Polygon translation broke relations.");
        });
    }

    private static Vertex V(double x, double y) => new(new Point2D(x, y));
    private static Polygon Square(double x, double y)
        => new(new[] { V(x, y), V(x + 100, y), V(x + 100, y + 100), V(x, y + 100) });

    private static (Scene Scene, Edge First, Edge Second) Pair()
    {
        var first = Square(0, 0);
        var second = Square(200, 50);
        return (new Scene(new[] { first, second }), first.Edges[0], second.Edges[0]);
    }

    private static (Scene Scene, Edge First, Edge Second) ConstrainedPair()
    {
        var (scene, first, second) = Pair();
        Require(ConstraintEditor.TrySetLength(scene, first, 100, out var message), message);
        Require(ConstraintEditor.TrySetLength(scene, second, 100, out message), message);
        Require(ConstraintEditor.TryAddParallel(scene, first, second, out message), message);
        return (scene, first, second);
    }

    private static void SamePositions(IReadOnlyDictionary<Vertex, Point2D> expected)
    {
        foreach (var (vertex, point) in expected)
        {
            Near(vertex.Position.X, point.X, 1e-10);
            Near(vertex.Position.Y, point.Y, 1e-10);
        }
    }

    private static void Near(double actual, double expected, double tolerance = ConstraintSolver.LengthTolerance)
        => Require(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance, $"Expected {expected}, got {actual}.");

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
